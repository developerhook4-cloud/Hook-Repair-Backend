using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Inventory.Application.Movements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/inventory/movements")]
[Authorize]
public sealed class InventoryMovementsController(
    IInventoryMovementService service,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetAll(
        Guid orgId,
        Guid? productId,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var movements = await service.GetAllAsync(
            orgId,
            productId,
            cancellationToken);

        return Ok(movements);
    }

    [HttpGet("{movementId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetById(
        Guid orgId,
        Guid movementId,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var movement = await service.GetByIdAsync(
            orgId,
            movementId,
            cancellationToken);

        return movement is null
            ? HookRepair.Api.Security.ProblemDetailsFactory.Create(
                404,
                "Inventory movement not found",
                "Inventory movement does not exist.")
            : Ok(movement);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateInventory)]
    public async Task<IActionResult> Create(
        Guid orgId,
        CreateInventoryMovementRequest? request,
        CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var validation = InventoryMovementRequestValidator.Validate(request);
        if (validation is not null)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                400,
                "Invalid inventory movement",
                validation);
        }

        try
        {
            var created = await service.CreateAsync(
                orgId,
                request!,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { orgId, movementId = created.Id },
                created);
        }
        catch (KeyNotFoundException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                404,
                "Product not found",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                400,
                "Invalid inventory movement",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                409,
                "Inventory conflict",
                exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                409,
                "Inventory conflict",
                "The inventory was modified by another request.");
        }
    }

    private IActionResult? EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(
                401,
                "Unauthorized",
                "A valid authenticated session is required.");
        }

        return tenantContext.OrganizationId == organizationId
            ? null
            : HookRepair.Api.Security.ProblemDetailsFactory.Create(
                403,
                "Forbidden",
                "User does not belong to this organization.");
    }
}