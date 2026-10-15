using System.ComponentModel.DataAnnotations;

namespace HookRepair.Modules.Auth.Application.Roles;

public sealed class SaveRoleRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;
    [Required]
    public string[] Permissions { get; init; } = [];
}

public sealed record RoleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions,
    bool Active
);

public sealed record RolesResponse(IReadOnlyList<RoleResponse> Roles);

public sealed record RoleMutationResponse(Guid Id, string Message);

public sealed record UpdateRoleStatusRequest(bool Active);