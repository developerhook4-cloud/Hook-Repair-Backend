using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.Roles;

public sealed class RoleManagementService : IRoleManagementService
{
    private readonly AuthDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public RoleManagementService(AuthDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        return await _dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<Role> CreateAsync(RoleDefinition definition, CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();
        var normalizedDefinition = Normalize(definition);

        var duplicateExists = await _dbContext.Roles.AnyAsync(
            role => role.Name.ToUpper() == normalizedDefinition.Name.ToUpper(),
            cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException("Un rol con el mismo nombre ya existe en la organización.");

        var role = new Role
        {
            OrganizationId = organizationId,
            Name = normalizedDefinition.Name,
            Permissions = normalizedDefinition.Permissions.ToArray(),
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return role;
    }

    public async Task<Role?> UpdateAsync(Guid roleId, RoleDefinition definition, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        if (role is null) return null;

        var normalizedDefinition = Normalize(definition);

        var duplicateExists = await _dbContext.Roles.AnyAsync(
            item => item.Id != roleId && item.Name.ToUpper() == normalizedDefinition.Name.ToUpper(),
            cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException("Un rol con el mismo nombre ya existe en la organización.");

        role.Name = normalizedDefinition.Name;
        role.Permissions = normalizedDefinition.Permissions.ToArray();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return role;
    }

    public async Task UpdateStatusAsync(Guid roleId, bool active, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(u => u.Id == roleId, cancellationToken);

        if (role is null) return;

        role.Active = active;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!_tenantContext.IsAuthenticated)
            throw new InvalidOperationException("An authenticated organization is required to manage roles.");

        return _tenantContext.OrganizationId;
    }

    private static RoleDefinition Normalize(RoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var name = definition.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A role name is required.", nameof(definition));

        var permissions = (definition.Permissions ?? [])
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invalidPermissions = permissions
            .Where(p => !SystemPermissions.ValidPermissions.Contains(p))
            .ToList();

        if (invalidPermissions.Count != 0)
        {
            throw new ArgumentException($"Los siguientes permisos no son válidos: {string.Join(", ", invalidPermissions)}");
        }

        return new RoleDefinition(name, permissions);
    }
}