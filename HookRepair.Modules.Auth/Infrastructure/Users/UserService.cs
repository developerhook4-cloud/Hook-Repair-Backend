using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Application.Users;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.Users
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IPasswordService _passwordService;
        private readonly ITenantContext _tenantContext;

        public UserService(AuthDbContext authDbContext, IPasswordService passwordService, ITenantContext tenantContext)
        {
            _authDbContext = authDbContext;
            _passwordService = passwordService;
            _tenantContext = tenantContext;
        }

        public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken)
        {
            var organizationId = EnsureAuthenticatedTenant();

            user.OrganizationId = organizationId;

            _authDbContext.Users.Add(user);
            await _authDbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken)
        {
            EnsureAuthenticatedTenant();

            return await _authDbContext.Users
                .Include(u => u.Role)
                .ToListAsync(cancellationToken);
        }

        public async Task<User> UpdateUserAsync(User user, Guid userId, string plainTextPassword, CancellationToken cancellationToken)
        {
            EnsureAuthenticatedTenant();

            var existingUser = await _authDbContext.Users
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (existingUser is null) return user;

            existingUser.Name = user.Name;
            existingUser.Email = user.Email;

            if (!string.IsNullOrWhiteSpace(plainTextPassword))
            {
                existingUser.PasswordHash = _passwordService.Hash(plainTextPassword);
            }

            existingUser.RoleId = user.RoleId;

            await _authDbContext.SaveChangesAsync(cancellationToken);
            return existingUser;
        }

        public async Task UpdateUserStatusAsync(Guid userId, bool active, CancellationToken cancellationToken)
        {
            EnsureAuthenticatedTenant();

            var existingUser = await _authDbContext.Users
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (existingUser is null) return;

            existingUser.IsActive = active;

            await _authDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _authDbContext.Users.FindAsync([userId], cancellationToken);

            if (user is null || !user.IsActive)
                return (false, "Usuario no encontrado o inactivo.");

            if (user.Email != request.Email.Trim().ToLowerInvariant())
            {
                var emailInUse = await _authDbContext.Users
                    .AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

                if (emailInUse)
                    return (false, "El correo electrónico ya está registrado por otro usuario.");

                user.Email = request.Email.Trim().ToLowerInvariant();
            }

            user.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(request.OldPassword))
                    return (false, "Debes ingresar tu contraseña actual para poder cambiarla.");

                if (!_passwordService.Verify(request.OldPassword, user.PasswordHash))
                    return (false, "La contraseña actual es incorrecta.");

                user.PasswordHash = _passwordService.Hash(request.NewPassword);
            }

            await _authDbContext.SaveChangesAsync(cancellationToken);

            return (true, null);
        }
        private Guid EnsureAuthenticatedTenant()
        {
            if (!_tenantContext.IsAuthenticated)
                throw new InvalidOperationException("Se requiere una organización autenticada para gestionar usuarios.");

            return _tenantContext.OrganizationId;
        }
    }
}