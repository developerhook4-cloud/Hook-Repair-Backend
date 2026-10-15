using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Inventory.Application.GoodsReceipts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/goods-receipts")]
[Authorize]
public sealed class GoodsReceiptController(
    IGoodsReceiptService service,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetAll(Guid orgId, [FromQuery] Guid? purchaseOrderId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        return Ok(await service.GetAllAsync(orgId, purchaseOrderId, cancellationToken));
    }

    [HttpGet("{goodsReceiptId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetById(Guid orgId, Guid goodsReceiptId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var result = await service.GetByIdAsync(orgId, goodsReceiptId, cancellationToken);
        return result is null
            ? HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Goods receipt not found", "Goods receipt does not exist.")
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateInventory)]
    public async Task<IActionResult> Create(Guid orgId, CreateGoodsReceiptRequest? request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var userId))
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "Authenticated user id is missing.");

        var validation = GoodsReceiptRequestValidator.Validate(request);
        if (validation is not null) return Invalid(validation);

        try
        {
            var created = await service.CreateAsync(orgId, userId, request!, cancellationToken);
            return CreatedAtAction(nameof(GetById),
                new { orgId, goodsReceiptId = created.Id },
                new GoodsReceiptMutationResponse(created.Id, "Recepción registrada exitosamente."));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Purchase order not found", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Goods receipt conflict", exception.Message);
        }
        catch (DbUpdateException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Goods receipt conflict", "Goods receipt could not be persisted.");
        }
    }

    private static IActionResult Invalid(string detail) =>
        HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid goods receipt", detail);

    private IActionResult? EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated)
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.");

        return tenantContext.OrganizationId == organizationId
            ? null
            : HookRepair.Api.Security.ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");
    }
}
