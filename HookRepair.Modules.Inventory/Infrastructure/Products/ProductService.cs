using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Inventory.Application.Products;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Inventory.Infrastructure.Products;

public sealed class ProductService(
    InventoryDbContext dbContext,
    ITenantContext tenantContext,
    ICategoryService categories,
    IBrandService brands,
    IModelService models,
    ISupplierService suppliers) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);

        var items = await dbContext.Products.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.BrandId,
                p.Brand != null ? p.Brand.Name : null,
                null,
                null,
                p.Models.Select(m => m.Model.Name).ToArray(),
                p.SupplierId,
                p.Supplier != null ? p.Supplier.Name : null,
                p.UPC,
                p.ImageUrl,
                p.Condition,
                p.InStock,
                p.StockWarning,
                p.MinimumReorderPoint,
                p.Price,
                p.GeneralSalePrice,
                p.DistributorSalePrice,
                p.IsSerialized,
                p.Serials.Select(s => s.IMEI).ToArray(),
                p.Active,
                p.InStock == 0 ? "Agotado" : (p.InStock <= p.StockWarning ? "Bajo Stock" : "En Stock")
            ))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Products.AsNoTracking()
            .Include(product => product.Models)
            .ThenInclude(m => m.Model)
            .SingleOrDefaultAsync(product => product.Id == productId && product.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<ProductResponse> CreateAsync(Guid organizationId, ProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var validation = ProductRequestValidator.ValidateCreate(request);
        if (validation is not null) throw new ArgumentException(validation);
        var modelIds = request.AllModelIds();
        await ValidateReferencesAsync(organizationId, request, modelIds, cancellationToken);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            IsSerialized = request.IsSerialized ?? false,
            Active = request.Active ?? true,
            InStock = request.InStock ?? 0
        };
        Apply(product, request);
        foreach (var modelId in modelIds)
            product.Models.Add(new ProductModel { Id = Guid.NewGuid(), OrganizationId = organizationId, ProductId = product.Id, ModelId = modelId });

        dbContext.Products.Add(product);
        if (product.InStock > 0)
        {
            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                OrganizationId = organizationId,
                ProductId = product.Id,
                Type = InventoryMovementType.Entry,
                Quantity = product.InStock,
                PreviousStock = 0,
                CurrentStock = product.InStock,
                Reason = "Stock inicial",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Producto, modelos y movimiento inicial en una sola transacción.
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(organizationId, product.Id, cancellationToken))!;
    }

    public async Task<ProductResponse?> UpdateAsync(Guid organizationId, Guid productId, ProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var validation = ProductRequestValidator.ValidateUpdate(request);
        if (validation is not null) throw new ArgumentException(validation);

        var product = await dbContext.Products.Include(item => item.Models)
            .SingleOrDefaultAsync(item => item.Id == productId && item.OrganizationId == organizationId, cancellationToken);
        if (product is null) return null;

        if (request.IsSerialized is not null && request.IsSerialized != product.IsSerialized)
            throw new ArgumentException("isSerialized cannot change after creation because it defines the UPC.");

        var modelIds = request.AllModelIds();
        await ValidateReferencesAsync(organizationId, request, modelIds, cancellationToken);
        Apply(product, request);

        var current = product.Models.Select(item => item.ModelId).ToHashSet();
        foreach (var removed in product.Models.Where(item => !modelIds.Contains(item.ModelId)).ToList())
            dbContext.ProductModels.Remove(removed);
        foreach (var added in modelIds.Where(id => !current.Contains(id)))
            dbContext.ProductModels.Add(new ProductModel { Id = Guid.NewGuid(), OrganizationId = organizationId, ProductId = product.Id, ModelId = added });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(organizationId, productId, cancellationToken);
    }

    public async Task<ProductResponse?> UpdateStatusAsync(Guid organizationId, Guid productId, bool active, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == productId && item.OrganizationId == organizationId, cancellationToken);
        if (product is null) return null;
        product.Active = active;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(organizationId, productId, cancellationToken);
    }

    // Campos editables en POST y PUT. InStock, Active, IsSerialized y UPC NO se tocan aquí.
    private static void Apply(Product product, ProductRequest request)
    {
        product.Name = request.Name!.Trim();
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.SupplierId = request.SupplierId;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.Condition = string.IsNullOrWhiteSpace(request.Condition) ? null : request.Condition.Trim();
        product.StockWarning = request.StockWarning ?? 0;
        product.MinimumReorderPoint = request.MinimumReorderPoint ?? 0;
        product.Price = request.Price!.Value;
        product.GeneralSalePrice = request.GeneralSalePrice!.Value;
        product.DistributorSalePrice = request.DistributorSalePrice!.Value;
    }

    private async Task ValidateReferencesAsync(Guid organizationId, ProductRequest request, IReadOnlyList<Guid> modelIds, CancellationToken cancellationToken)
    {
        if (request.CategoryId is { } categoryId &&
            (await categories.GetByIdAsync(organizationId, categoryId, cancellationToken)) is not { Active: true })
            throw new ArgumentException("categoryId does not exist or is inactive.");

        if (request.BrandId is { } brandId &&
            (await brands.GetByIdAsync(organizationId, brandId, cancellationToken)) is not { Active: true })
            throw new ArgumentException("brandId does not exist or is inactive.");

        if (request.SupplierId is { } supplierId &&
            (await suppliers.GetByIdAsync(organizationId, supplierId, cancellationToken)) is not { Active: true })
            throw new ArgumentException("supplierId does not exist or is inactive.");

        foreach (var modelId in modelIds)
        {
            var model = await models.GetByIdAsync(organizationId, modelId, cancellationToken);
            if (model is not { Active: true })
                throw new ArgumentException("modelId does not exist or is inactive.");
            if (request.BrandId is not null && model.BrandId != request.BrandId)
                throw new ArgumentException("modelId does not belong to brandId.");
        }
    }

    private void EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId)
            throw new TenantAccessException();
    }

    private static ProductResponse Map(Product item)
    {
        var modelIds = item.Models.Select(model => model.ModelId).OrderBy(id => id).ToArray();
        return new ProductResponse(
            item.Id,
            item.Name,
            item.CategoryId,
            item.Category != null ? item.Category.Name : null,
            item.BrandId,
            item.Brand != null ? item.Brand.Name : null,
            null,
            null,
            item.Models.Select(m => m.Model.Name).ToArray(),
            item.SupplierId,
            item.Supplier != null ? item.Supplier.Name : null,
            item.UPC,
            item.ImageUrl,
            item.Condition,
            item.InStock,
            item.StockWarning,
            item.MinimumReorderPoint,
            item.Price,
            item.GeneralSalePrice,
            item.DistributorSalePrice,
            item.IsSerialized,
            item.Serials.Select(s => s.IMEI).ToArray(),
            item.Active,
            item.InStock == 0 ? "Agotado" : (item.InStock <= item.StockWarning ? "Bajo Stock" : "En Stock")
        );
    }
}
