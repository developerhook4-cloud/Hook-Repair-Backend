using HookRepair.Modules.Auth.Application.Organizations;
using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Modules.Auth.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public OrganizationService(AuthDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<Organization> CreateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _dbContext.Organizations.Add(organization);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return organization;
    }

    public async Task<User> CreateOrganizationUserAsync(User user, string plainTextPassword, CancellationToken cancellationToken = default)
    {
        if (user.RoleId == Guid.Empty)
        {
            if (user.RoleId == Guid.Empty)
            {
                var adminRole = await _dbContext.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r =>
                        r.OrganizationId == user.OrganizationId &&
                        r.Name == "Administrador",
                        cancellationToken);

                if (adminRole is null)
                {
                    throw new InvalidOperationException("No se encontró el rol de Administrador. Asegúrate de generar los roles de la empresa antes de crear al usuario.");
                }

                user.RoleId = adminRole.Id;
            }
        }
        // recibir contraseña en texto plano y hashearla antes de guardarla en la base de datos
        user.PasswordHash = _passwordService.Hash(plainTextPassword);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task UpdateOrganizationUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.IsActive, isActive), cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetAllOrganizations(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllOrganizationUsers(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId)
            .Include(u => u.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var existingOrg = await _dbContext.Organizations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(o => o.Id == organization.Id, cancellationToken);

        if (existingOrg is null) return organization;

        existingOrg.Name = organization.Name;
        existingOrg.Email = organization.Email;
        existingOrg.Website = organization.Website;
        existingOrg.Phone1 = organization.Phone1;
        existingOrg.Phone2 = organization.Phone2;
        existingOrg.Address = organization.Address;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingOrg;
    }

    public async Task<Organization> UpdateOrganizationStatusAsync(Guid OrganizationId, bool isActive, CancellationToken cancellationToken = default)
    {
        var existingOrg = await _dbContext.Organizations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(o => o.Id == OrganizationId, cancellationToken);
        if (existingOrg is null) return null;
        existingOrg.Active = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingOrg;
    }

    public async Task<User> UpdateOrganizationUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.Name, user.Name)
                .SetProperty(p => p.Email, user.Email)
                .SetProperty(p => p.IsActive, user.IsActive)
                .SetProperty(p => p.RoleId, user.RoleId), cancellationToken);

        return user;
    }
}