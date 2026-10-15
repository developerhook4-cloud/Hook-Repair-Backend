using HookRepair.Modules.Auth.Core.Entities;
namespace HookRepair.Modules.Auth.Application.Organizations
{
    public interface IOrganizationService
    {
        public Task<Organization> CreateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);
        public Task<IEnumerable<Organization>> GetAllOrganizations(CancellationToken cancellationToken = default);
        public Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);
        public Task<Organization> UpdateOrganizationStatusAsync(Guid organizationId, bool isActive, CancellationToken cancellationToken = default);
        public Task<User> CreateOrganizationUserAsync(User user, string plainTextPassword, CancellationToken cancellationToken = default);
        public Task<User> UpdateOrganizationUserAsync(User user, CancellationToken cancellationToken = default);
        public Task UpdateOrganizationUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
        public Task<IEnumerable<User>> GetAllOrganizationUsers(Guid organizationId, CancellationToken cancellationToken = default);
    }
}
