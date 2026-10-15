using HookRepair.Api.Security;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.ThirdParties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/clients")]
[Authorize]
public sealed class ClientController(IClientService service, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewCustomers)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        return error ?? Ok(await service.GetAllAsync(orgId, cancellationToken));
    }

    [HttpGet("{clientId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewCustomers)]
    public async Task<IActionResult> GetById(Guid orgId, Guid clientId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var result = await service.GetByIdAsync(orgId, clientId, cancellationToken);
        return result is null ? ProblemDetailsFactory.Create(404, "Client not found", "Client does not exist.") : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateCustomers)]
    public async Task<IActionResult> Create(Guid orgId, CreateClientRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = ThirdPartyRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid client", validation);
        try
        {
            var created = await service.CreateAsync(orgId, request!, cancellationToken);
            return Ok(new ClientMutationResponse(created.Id, "Cliente creado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid client", exception.Message); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Client conflict", "Client already exists."); }
    }

    [HttpPut("{clientId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateCustomers)]
    public async Task<IActionResult> Update(Guid orgId, Guid clientId, UpdateClientRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = ThirdPartyRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid client", validation);
        try
        {
            var updated = await service.UpdateAsync(orgId, clientId, request!, cancellationToken);
            return updated is null ? ProblemDetailsFactory.Create(404, "Client not found", "Client does not exist.") : Ok(new ClientMutationResponse(updated.Id, "Cliente actualizado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid client", exception.Message); }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Client conflict", "Client was modified by another request."); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Client conflict", "Client already exists."); }
    }

    [HttpPatch("{clientId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteCustomers)]
    public async Task<IActionResult> Deactivate(Guid orgId, Guid clientId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        try
        {
            var result = await service.DeactivateAsync(orgId, clientId, cancellationToken);
            return result is null ? ProblemDetailsFactory.Create(404, "Client not found", "Client does not exist.") : Ok(new ClientMutationResponse(result.Id, "Estado del cliente actualizado exitosamente."));
        }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Client conflict", "Client was modified by another request."); }
    }

    private IActionResult? EnsureTenant(Guid organizationId) => !tenantContext.IsAuthenticated
        ? ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.")
        : tenantContext.OrganizationId == organizationId ? null : ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");

    private new static class ProblemDetailsFactory
    {
        public static ObjectResult Create(int statusCode, string title, string detail) => HookRepair.Api.Security.ProblemDetailsFactory.Create(statusCode, title, detail);
    }
}
