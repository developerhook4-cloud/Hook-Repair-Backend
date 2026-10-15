using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Inventory.Application.PurchaseOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/purchase-orders")]
[Authorize]
public sealed class PurchaseOrderController(
    IPurchaseOrderService service,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        return Ok(await service.GetAllAsync(orgId, cancellationToken));
    }

    [HttpGet("{purchaseOrderId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetById(Guid orgId, Guid purchaseOrderId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var result = await service.GetByIdAsync(orgId, purchaseOrderId, cancellationToken);
        return result is null
            ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Purchase order not found", "Purchase order does not exist.")
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateInventory)]
    public async Task<IActionResult> Create(Guid orgId, CreatePurchaseOrderRequest? request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var userId))
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "Authenticated user id is missing.");

        var validation = PurchaseOrderRequestValidator.Validate(request);
        if (validation is not null) return Invalid(validation);

        try
        {
            var created = await service.CreateAsync(orgId, userId, request!, cancellationToken);
            return CreatedAtAction(nameof(GetById),
                new { orgId, purchaseOrderId = created.Id },
                new PurchaseOrderMutationResponse(created.Id, "Orden de compra creada exitosamente."));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (DbUpdateException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Purchase order conflict", "Purchase order could not be persisted.");
        }
    }

    private static IActionResult Invalid(string detail) =>
        HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid purchase order", detail);

    private IActionResult? EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated)
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.");

        return tenantContext.OrganizationId == organizationId
            ? null
            : HookRepair.Api.Security.ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");
    }
}
