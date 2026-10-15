using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/roles")]
[Authorize]
public sealed class RoleController(
    IRoleManagementService roleService,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewRoles)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var roles = await roleService.GetAllAsync(cancellationToken);

        var response = roles.Select(r => new RoleResponse(r.Id, r.Name, r.Permissions, r.Active)).ToList();
        return Ok(new RolesResponse(response));
    }

    [HttpGet("{roleId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewRoles)]
    public async Task<IActionResult> GetById(Guid orgId, Guid roleId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var role = await roleService.GetByIdAsync(roleId, cancellationToken);

        return role is null
            ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Role not found", "Role does not exist.")
            : Ok(new RoleResponse(role.Id, role.Name, role.Permissions, role.Active));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateRoles)]
    public async Task<IActionResult> Create(Guid orgId, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        try
        {
            var definition = new RoleDefinition(request.Name, request.Permissions);
            var role = await roleService.CreateAsync(definition, cancellationToken);

            return CreatedAtAction(nameof(GetById),
                new { orgId, roleId = role.Id },
                new RoleMutationResponse(role.Id, "Rol creado correctamente."));
        }
        catch (ArgumentException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid role", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Role conflict", exception.Message);
        }
    }

    [HttpPut("{roleId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateRoles)]
    public async Task<IActionResult> Update(Guid orgId, Guid roleId, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        try
        {
            var definition = new RoleDefinition(request.Name, request.Permissions);
            var role = await roleService.UpdateAsync(roleId, definition, cancellationToken);

            return role is null
                ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Role not found", "Role does not exist.")
                : Ok(new RoleMutationResponse(role.Id, "Rol actualizado correctamente."));
        }
        catch (ArgumentException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid role", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Role conflict", exception.Message);
        }
    }

    [HttpPatch("{roleId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteRoles)]
    public async Task<IActionResult> UpdateStatus(Guid orgId, Guid roleId, [FromBody] UpdateRoleStatusRequest request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        await roleService.UpdateStatusAsync(roleId, request.Active, cancellationToken);

        return Ok(new RoleMutationResponse(roleId, "Estado del rol actualizado correctamente."));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = SystemPermissions.ViewRoles)]
    public IActionResult GetAllPermissions(Guid orgId)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        return Ok(SystemPermissions.ValidPermissions);
    }

    private IActionResult? EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated)
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.");

        return tenantContext.OrganizationId == organizationId
            ? null
            : HookRepair.Api.Security.ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");
    }
}