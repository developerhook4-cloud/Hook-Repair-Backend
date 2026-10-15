using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Catalogs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Catalogs.Infrastructure.Catalogs;

public sealed class BrandService(CatalogDbContext dbContext, ITenantContext tenantContext) : IBrandService
{
    public async Task<IReadOnlyList<BrandResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var items = await dbContext.Brands.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<BrandResponse?> GetByIdAsync(Guid organizationId, Guid brandId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Brands.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == brandId && item.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<BrandResponse> CreateAsync(Guid organizationId, CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = new Brand { OrganizationId = organizationId, Name = request.Name!.Trim(), Active = true };
        dbContext.Brands.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<BrandResponse?> UpdateAsync(Guid organizationId, Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = await dbContext.Brands
            .SingleOrDefaultAsync(item => item.Id == brandId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Name = request.Name!.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<BrandResponse?> DeactivateAsync(Guid organizationId, Guid brandId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Brands
            .SingleOrDefaultAsync(item => item.Id == brandId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static void Validate(CreateBrandRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void Validate(UpdateBrandRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void ThrowIfInvalid(string? error) { if (error is not null) throw new ArgumentException(error); }
    private void EnsureTenant(Guid organizationId) { if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId) throw new TenantAccessException(); }
    private static BrandResponse Map(Brand item) => new(item.Id, item.Name, item.Active);
}
