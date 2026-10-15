using HookRepair.Modules.Auth.Core.Entities;

namespace HookRepair.Modules.Auth.Application.Roles;

public sealed record RoleDefinition(
    string Name,
    IReadOnlyCollection<string> Permissions);

public interface IRoleManagementService
{
    Task<IReadOnlyList<Role>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Role?> GetByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<Role> CreateAsync(
        RoleDefinition definition,
        CancellationToken cancellationToken = default);

    Task<Role?> UpdateAsync(
        Guid roleId,
        RoleDefinition definition,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid roleId,
        bool active,
        CancellationToken cancellationToken= default);
}