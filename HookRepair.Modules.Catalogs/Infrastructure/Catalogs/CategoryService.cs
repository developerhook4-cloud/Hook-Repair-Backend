using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Catalogs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Catalogs.Infrastructure.Catalogs;

public sealed class CategoryService(CatalogDbContext dbContext, ITenantContext tenantContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var items = await dbContext.Categories.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid organizationId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == categoryId && item.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<CategoryResponse> CreateAsync(Guid organizationId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = new Category { OrganizationId = organizationId, Name = request.Name!.Trim(), Icon = request.Icon.Trim(), Active = true };
        dbContext.Categories.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<CategoryResponse?> UpdateAsync(Guid organizationId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == categoryId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Name = request.Name!.Trim();
        item.Icon = request.Icon!.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<CategoryResponse?> DeactivateAsync(Guid organizationId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == categoryId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static void Validate(CreateCategoryRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void Validate(UpdateCategoryRequest request) => ThrowIfInvalid(CatalogRequestValidator.Validate(request));
    private static void ThrowIfInvalid(string? error) { if (error is not null) throw new ArgumentException(error); }
    private void EnsureTenant(Guid organizationId) { if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId) throw new TenantAccessException(); }
    private static CategoryResponse Map(Category item) => new(item.Id, item.Name, item.Icon, item.Active);
}
