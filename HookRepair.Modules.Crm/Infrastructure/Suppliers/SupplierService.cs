using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Application.ThirdParties;
using HookRepair.Modules.Crm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Crm.Infrastructure.Suppliers;

public sealed class SupplierService(CrmDbContext dbContext, ITenantContext tenantContext) : ISupplierService
{
    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var items = await dbContext.Suppliers.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid organizationId, Guid supplierId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Suppliers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == supplierId && item.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<SupplierResponse> CreateAsync(Guid organizationId, CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = new Supplier
        {
            OrganizationId = organizationId,
            Name = request.Name!.Trim(),
            Phone1 = request.Phone1!.Trim(),
            Phone2 = Normalize(request.Phone2),
            Email = request.Email!.Trim(),
            Active = true
        };
        dbContext.Suppliers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid organizationId, Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = await dbContext.Suppliers
            .SingleOrDefaultAsync(item => item.Id == supplierId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Name = request.Name!.Trim();
        item.Phone1 = request.Phone1!.Trim();
        item.Phone2 = Normalize(request.Phone2);
        item.Email = request.Email!.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<SupplierResponse?> DeactivateAsync(Guid organizationId, Guid supplierId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Suppliers
            .SingleOrDefaultAsync(item => item.Id == supplierId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static void Validate(CreateSupplierRequest request) => ThrowIfInvalid(ThirdPartyRequestValidator.Validate(request));
    private static void Validate(UpdateSupplierRequest request) => ThrowIfInvalid(ThirdPartyRequestValidator.Validate(request));
    private static void ThrowIfInvalid(string? error) { if (error is not null) throw new ArgumentException(error); }
    private void EnsureTenant(Guid organizationId) { if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId) throw new TenantAccessException(); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SupplierResponse Map(Supplier item) => new(item.Id, item.Name, item.Phone1, item.Phone2, item.Email, item.Active);
}
