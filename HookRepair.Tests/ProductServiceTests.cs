using FluentAssertions;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Inventory.Application.Products;
using HookRepair.Modules.Inventory.Core.Entities;
using HookRepair.Modules.Inventory.Infrastructure;
using HookRepair.Modules.Inventory.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class ProductServiceTests
{
    private static ProductRequest Valid(int inStock = 0, Guid? modelId = null, Guid? brandId = null,
        Guid? supplierId = null, bool isSerialized = false, string name = " Pantalla ") =>
        new(name, null, brandId, modelId, null, supplierId, "ignored", null, null, inStock, 2, 1,
            100m, 200m, 150m, isSerialized, true);

    [Fact]
    public async Task Create_accepts_frontend_payload_without_supplier()
    {
        var (service, db, orgId, _) = Create();

        var result = await service.CreateAsync(orgId, Valid());

        result.Name.Should().Be("Pantalla");
        result.SupplierId.Should().BeNull();
        result.Active.Should().BeTrue();
        (await db.Products.SingleAsync()).OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task Create_with_initial_stock_records_entry_movement()
    {
        var (service, db, orgId, _) = Create();

        var result = await service.CreateAsync(orgId, Valid(inStock: 5));

        result.InStock.Should().Be(5);
        var movement = await db.InventoryMovements.SingleAsync();
        movement.Type.Should().Be(InventoryMovementType.Entry);
        movement.Quantity.Should().Be(5);
        movement.PreviousStock.Should().Be(0);
        movement.CurrentStock.Should().Be(5);
        movement.Reason.Should().Be("Stock inicial");
    }

    [Fact]
    public async Task Create_rejects_unknown_or_inactive_supplier()
    {
        var (service, _, orgId, refs) = Create();
        var inactive = Guid.NewGuid();
        refs.Suppliers[inactive] = false;

        await FluentActions.Invoking(() => service.CreateAsync(orgId, Valid(supplierId: Guid.NewGuid())))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*supplierId*");
        await FluentActions.Invoking(() => service.CreateAsync(orgId, Valid(supplierId: inactive)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*supplierId*");
    }

    [Fact]
    public async Task Create_rejects_model_from_other_brand()
    {
        var (service, _, orgId, refs) = Create();
        var brand = Guid.NewGuid();
        var model = Guid.NewGuid();
        refs.Brands[brand] = true;
        refs.Models[model] = (Guid.NewGuid(), true);

        await FluentActions.Invoking(() => service.CreateAsync(orgId, Valid(modelId: model, brandId: brand)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*modelId*");
    }

    [Fact]
    public async Task Create_links_model_and_returns_it_as_model_id()
    {
        var (service, _, orgId, refs) = Create();
        var brand = Guid.NewGuid();
        var model = Guid.NewGuid();
        refs.Brands[brand] = true;
        refs.Models[model] = (brand, true);

        var result = await service.CreateAsync(orgId, Valid(modelId: model, brandId: brand));

        result.ModelId.Should().Be(model);
        result.ModelIds.Should().Equal(model);
    }

    [Fact]
    public async Task Update_ignores_in_stock_and_active()
    {
        var (service, db, orgId, _) = Create();
        var created = await service.CreateAsync(orgId, Valid(inStock: 5));

        var updated = await service.UpdateAsync(orgId, created.Id,
            Valid(inStock: 999, name: "Nuevo") with { Active = false });

        updated!.Name.Should().Be("Nuevo");
        updated.InStock.Should().Be(5);
        updated.Active.Should().BeTrue();
        (await db.InventoryMovements.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Update_rejects_changing_is_serialized()
    {
        var (service, _, orgId, _) = Create();
        var created = await service.CreateAsync(orgId, Valid());

        await FluentActions.Invoking(() => service.UpdateAsync(orgId, created.Id, Valid(isSerialized: true)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*isSerialized*");
    }

    [Fact]
    public async Task GetById_returns_null_for_other_tenant_product()
    {
        var (service, db, orgId, _) = Create();
        db.Products.Add(new Product { OrganizationId = Guid.NewGuid(), Name = "Ajeno", Active = true });
        await db.SaveChangesAsync();
        var foreignId = (await db.Products.IgnoreQueryFilters().SingleAsync()).Id;

        (await service.GetByIdAsync(orgId, foreignId)).Should().BeNull();
        (await service.GetAllAsync(orgId)).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_deactivates_without_deleting()
    {
        var (service, db, orgId, _) = Create();
        var created = await service.CreateAsync(orgId, Valid());

        var result = await service.UpdateStatusAsync(orgId, created.Id, false);

        result!.Active.Should().BeFalse();
        (await db.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Operations_reject_mismatched_organization()
    {
        var (service, _, _, _) = Create();

        await FluentActions.Invoking(() => service.GetAllAsync(Guid.NewGuid()))
            .Should().ThrowAsync<TenantAccessException>();
    }

    private static (ProductService Service, InventoryDbContext Db, Guid OrgId, FakeReferences Refs) Create()
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        var db = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        var refs = new FakeReferences();
        return (new ProductService(db, tenant, refs, refs, refs, refs), db, orgId, refs);
    }

    private sealed class FakeReferences : ICategoryService, IBrandService, IModelService, ISupplierService
    {
        public Dictionary<Guid, bool> Categories { get; } = new();
        public Dictionary<Guid, bool> Brands { get; } = new();
        public Dictionary<Guid, (Guid BrandId, bool Active)> Models { get; } = new();
        public Dictionary<Guid, bool> Suppliers { get; } = new();

        Task<CategoryResponse?> ICategoryService.GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult(Categories.TryGetValue(id, out var a) ? new CategoryResponse(id, "C", "", a) : null);
        Task<BrandResponse?> IBrandService.GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult(Brands.TryGetValue(id, out var a) ? new BrandResponse(id, "B", a) : null);
        Task<ModelResponse?> IModelService.GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult(Models.TryGetValue(id, out var m) ? new ModelResponse(id, m.BrandId, "B", "M", m.Active) : null);
        Task<SupplierResponse?> ISupplierService.GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult(Suppliers.TryGetValue(id, out var a) ? new SupplierResponse(id, "S", "1", null, "s@x.com", a) : null);

        // Miembros no usados por ProductService.
        Task<IReadOnlyList<CategoryResponse>> ICategoryService.GetAllAsync(Guid o, CancellationToken c) => throw new NotSupportedException();
        Task<CategoryResponse> ICategoryService.CreateAsync(Guid o, CreateCategoryRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<CategoryResponse?> ICategoryService.UpdateAsync(Guid o, Guid id, UpdateCategoryRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<CategoryResponse?> ICategoryService.DeactivateAsync(Guid o, Guid id, CancellationToken c) => throw new NotSupportedException();
        Task<IReadOnlyList<BrandResponse>> IBrandService.GetAllAsync(Guid o, CancellationToken c) => throw new NotSupportedException();
        Task<BrandResponse> IBrandService.CreateAsync(Guid o, CreateBrandRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<BrandResponse?> IBrandService.UpdateAsync(Guid o, Guid id, UpdateBrandRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<BrandResponse?> IBrandService.DeactivateAsync(Guid o, Guid id, CancellationToken c) => throw new NotSupportedException();
        Task<IReadOnlyList<ModelResponse>> IModelService.GetAllAsync(Guid o, CancellationToken c) => throw new NotSupportedException();
        Task<ModelResponse> IModelService.CreateAsync(Guid o, CreateModelRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<ModelResponse?> IModelService.UpdateAsync(Guid o, Guid id, UpdateModelRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<ModelResponse?> IModelService.DeactivateAsync(Guid o, Guid id, CancellationToken c) => throw new NotSupportedException();
        Task<IReadOnlyList<SupplierResponse>> ISupplierService.GetAllAsync(Guid o, CancellationToken c) => throw new NotSupportedException();
        Task<SupplierResponse> ISupplierService.CreateAsync(Guid o, CreateSupplierRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<SupplierResponse?> ISupplierService.UpdateAsync(Guid o, Guid id, UpdateSupplierRequest r, CancellationToken c) => throw new NotSupportedException();
        Task<SupplierResponse?> ISupplierService.DeactivateAsync(Guid o, Guid id, CancellationToken c) => throw new NotSupportedException();
    }
}
