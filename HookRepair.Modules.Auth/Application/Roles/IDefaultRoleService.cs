namespace HookRepair.Modules.Auth.Application.Roles;

public interface IDefaultRoleService
{
    Task RegisterAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
