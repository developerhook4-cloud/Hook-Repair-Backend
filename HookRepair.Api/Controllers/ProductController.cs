using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Inventory.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/products")]
[Authorize]
public sealed class ProductController(
    IProductService service,
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

    [HttpGet("{productId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewInventory)]
    public async Task<IActionResult> GetById(Guid orgId, Guid productId, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var result = await service.GetByIdAsync(orgId, productId, cancellationToken);
        return result is null ? NotFoundProblem() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateInventory)]
    public async Task<IActionResult> Create(Guid orgId, ProductRequest? request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var validation = ProductRequestValidator.ValidateCreate(request);
        if (validation is not null) return Invalid(validation);

        try
        {
            var created = await service.CreateAsync(orgId, request!, cancellationToken);
            return CreatedAtAction(nameof(GetById),
                new { orgId, productId = created.Id },
                new ProductMutationResponse(created.Id, "Producto creado exitosamente."));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (DbUpdateException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Product conflict", "Product could not be persisted.");
        }
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateInventory)]
    public async Task<IActionResult> Update(Guid orgId, Guid productId, ProductRequest? request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var validation = ProductRequestValidator.ValidateUpdate(request);
        if (validation is not null) return Invalid(validation);

        try
        {
            var updated = await service.UpdateAsync(orgId, productId, request!, cancellationToken);
            return updated is null
                ? NotFoundProblem()
                : Ok(new ProductMutationResponse(updated.Id, "Producto actualizado exitosamente."));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (DbUpdateException)
        {
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(409, "Product conflict", "Product was modified by another request.");
        }
    }

    [HttpPatch("{productId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteInventory)]
    public async Task<IActionResult> UpdateStatus(Guid orgId, Guid productId, [FromBody] UpdateProductStatusRequest request, CancellationToken cancellationToken)
    {
        var tenantError = EnsureTenant(orgId);
        if (tenantError is not null) return tenantError;

        var updated = await service.UpdateStatusAsync(orgId, productId, request.Active, cancellationToken);
        return updated is null
            ? NotFoundProblem()
            : Ok(new ProductMutationResponse(productId, "Estatus del producto actualizado exitosamente."));
    }

    private static IActionResult NotFoundProblem() =>
        HookRepair.Api.Security.ProblemDetailsFactory.Create(404, "Product not found", "Product does not exist.");

    private static IActionResult Invalid(string detail) =>
        HookRepair.Api.Security.ProblemDetailsFactory.Create(400, "Invalid product", detail);

    private IActionResult? EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated)
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.");

        return tenantContext.OrganizationId == organizationId
            ? null
            : HookRepair.Api.Security.ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");
    }
}
