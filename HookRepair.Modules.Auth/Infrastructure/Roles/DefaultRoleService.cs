using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.Roles;

public sealed class DefaultRoleService : IDefaultRoleService
{
    private static readonly Dictionary<string, string[]> DefaultRolesWithPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Administrador",
            [
                "Users.View", "Users.Update", "Users.Delete",
                "Roles.View", "Roles.Update", "Roles.Delete",
                "Pos.View", "Pos.Update", "Pos.Delete",
                "Repairs.View", "Repairs.Update", "Repairs.Delete",
                "Inventory.View", "Inventory.Update", "Inventory.Delete",
                "Customers.View", "Customers.Update", "Customers.Delete",
                "Suppliers.View", "Suppliers.Update", "Suppliers.Delete",
                "Catalogs.View", "Catalogs.Update", "Catalogs.Delete",
                "Devices.View", "Devices.Update", "Devices.Delete",
                "Services.View", "Services.Update", "Services.Delete",
                "RepairStatus.View", "RepairStatus.Update", "RepairStatus.Delete",
                "Reports.View", "Reports.Update", "Reports.Delete"
            ]
        },
        {
            "Gerente",
            [
                "Users.View",
                "Pos.View", "Pos.Update", "Pos.Delete",
                "Repairs.View", "Repairs.Update", "Repairs.Delete",
                "Inventory.View", "Inventory.Update", "Inventory.Delete",
                "Customers.View", "Customers.Update", "Customers.Delete",
                "Suppliers.View", "Suppliers.Update", "Suppliers.Delete",
                "Catalogs.View", "Catalogs.Update", "Catalogs.Delete",
                "Devices.View", "Devices.Update", "Devices.Delete",
                "Services.View", "Services.Update", "Services.Delete",
                "RepairStatus.View", "RepairStatus.Update", "RepairStatus.Delete",
                "Reports.View", "Reports.Update", "Reports.Delete"
            ]
        },
        {
            "Técnico",
            [
                "Repairs.View", "Repairs.Update",
                "Inventory.View", "Inventory.Update",
                "Devices.View", "Devices.Update",
                "Services.View", "Services.Update",
                "Customers.View"
            ]
        },
        {
            "Cajero",
            [
                "Pos.View", "Pos.Update",
                "Repairs.View", "Repairs.Update",
                "Inventory.View", "Inventory.Update",
                "Customers.View", "Customers.Update"
            ]
        }
    };

    private readonly AuthDbContext _dbContext;

    public DefaultRoleService(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var existingRoleNames = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(role => role.OrganizationId == organizationId)
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        var rolesToAdd = new List<Role>();

        foreach (var defaultRole in DefaultRolesWithPermissions)
        {
            if (!existingRoleNames.Contains(defaultRole.Key, StringComparer.OrdinalIgnoreCase))
            {
                var newRole = new Role
                {
                    OrganizationId = organizationId,
                    Name = defaultRole.Key,

                    Permissions = defaultRole.Value
                };

                rolesToAdd.Add(newRole);
            }
        }

        if (rolesToAdd.Any())
        {
            await _dbContext.Roles.AddRangeAsync(rolesToAdd, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}