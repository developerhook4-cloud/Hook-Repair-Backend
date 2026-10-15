using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Catalogs.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;


[ApiController]
[Route("api/{orgId:guid}/services")]
[Authorize]
public sealed class ServicesController(
    IServiceService service,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewServices)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{serviceId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewServices)]
    public async Task<IActionResult> GetById(Guid orgId, Guid serviceId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var result = await service.GetByIdAsync(serviceId, cancellationToken);
        return result is null
            ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Service not found", "Service does not exist.")
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateServices)]
    public async Task<IActionResult> Create(
        Guid orgId,
        SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        try
        {
            var created = await service.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById),
                new { orgId, serviceId = created.Id },
                new ServiceMutationResponse(created.Id, "Servicio creado exitosamente."));
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid service", exception.Message);
        }
        catch (DbUpdateException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Service conflict", "Service could not be persisted.");
        }
    }

    [HttpPut("{serviceId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateServices)]
    public async Task<IActionResult> Update(
        Guid orgId,
        Guid serviceId,
        SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        try
        {
            var updated = await service.UpdateAsync(serviceId, request, cancellationToken);
            return updated is null
                ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Service not found", "Service does not exist.")
                : Ok(new ServiceMutationResponse(updated.Id, "Servicio actualizado exitosamente."));
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid service", exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Service conflict", "Service was modified by another request.");
        }
    }

    [HttpPatch("{serviceId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteServices)]
    public async Task<IActionResult> UpdateStatus(
        Guid orgId,
        Guid serviceId,
        [FromBody] UpdateServiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        try
        {
            await service.UpdateStatusAsync(serviceId, request.Active, cancellationToken);
            return Ok(new ServiceMutationResponse(serviceId, "Estatus del servicio actualizado exitosamente."));
        }
        catch (DbUpdateConcurrencyException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Service conflict", "Service was modified by another request.");
        }
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