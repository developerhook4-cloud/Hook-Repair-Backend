using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Catalogs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Catalogs.Infrastructure.Catalogs;

public sealed class ModelService(CatalogDbContext dbContext, ITenantContext tenantContext) : IModelService
{
    public async Task<IReadOnlyList<ModelResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var items = await dbContext.Models.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .Include(item => item.Brand)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<ModelResponse?> GetByIdAsync(Guid organizationId, Guid modelId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Models.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == modelId && item.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<ModelResponse> CreateAsync(Guid organizationId, CreateModelRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        await EnsureBrandAsync(organizationId, request.BrandId, cancellationToken);
        var item = new Model { OrganizationId = organizationId, BrandId = request.BrandId, Name = request.Name!.Trim(), Active = true };
        dbContext.Models.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ModelResponse?> UpdateAsync(Guid organizationId, Guid modelId, UpdateModelRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        await EnsureBrandAsync(organizationId, request.BrandId, cancellationToken);
        var item = await dbContext.Models
            .SingleOrDefaultAsync(item => item.Id == modelId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.BrandId = request.BrandId;
        item.Name = request.Name!.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ModelResponse?> DeactivateAsync(Guid organizationId, Guid modelId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Models
            .SingleOrDefaultAsync(item => item.Id == modelId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private async Task EnsureBrandAsync(Guid organizationId, Guid brandId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Brands.AnyAsync(item => item.Id == brandId && item.OrganizationId == organizationId, cancellationToken))
            throw new ArgumentException("brandId must reference a brand from this organization.", nameof(brandId));
    }

    private static void Validate(CreateModelRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void Validate(UpdateModelRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void ThrowIfInvalid(string? error) { if (error is not null) throw new ArgumentException(error); }
    private void EnsureTenant(Guid organizationId) { if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId) throw new TenantAccessException(); }
    private static ModelResponse Map(Model item) => new(item.Id, item.BrandId, item.BrandName, item.Name, item.Active);
}
