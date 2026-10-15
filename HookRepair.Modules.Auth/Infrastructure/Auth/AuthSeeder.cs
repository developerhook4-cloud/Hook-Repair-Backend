using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure;

public class AuthSeeder
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public AuthSeeder(AuthDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "admin@hookrepair.com", cancellationToken))
        {
            return;
        }

        var rootOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Hook Technologies",
            Email = "admin@hookrepair.com",
            Active = true
        };
        _dbContext.Organizations.Add(rootOrganization);

        var superAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = rootOrganization.Id,
            Name = "Super Administrador",
            Permissions = [
                "Organizations.View", "Organizations.Update", "Organizations.Delete",
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
        };
        _dbContext.Roles.Add(superAdminRole);

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = rootOrganization.Id,
            RoleId = superAdminRole.Id,
            Name = "Super Admin",
            Email = "admin@hookrepair.com",
            PasswordHash = _passwordService.Hash("AdminPassword123"),
            IsActive = true
        };
        _dbContext.Users.Add(superAdmin);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
