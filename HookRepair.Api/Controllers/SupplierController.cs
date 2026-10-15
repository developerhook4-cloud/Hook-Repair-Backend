using HookRepair.Api.Security;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Application.ThirdParties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/suppliers")]
[Authorize]
public sealed class SupplierController(ISupplierService service, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewSuppliers)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        return error ?? Ok(await service.GetAllAsync(orgId, cancellationToken));
    }

    [HttpGet("{supplierId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewSuppliers)]
    public async Task<IActionResult> GetById(Guid orgId, Guid supplierId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var result = await service.GetByIdAsync(orgId, supplierId, cancellationToken);
        return result is null ? ProblemDetailsFactory.Create(404, "Supplier not found", "Supplier does not exist.") : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateSuppliers)]
    public async Task<IActionResult> Create(Guid orgId, CreateSupplierRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = ThirdPartyRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid supplier", validation);
        try
        {
            var created = await service.CreateAsync(orgId, request!, cancellationToken);
            return Ok(new SupplierMutationResponse(created.Id, "Proveedor creado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid supplier", exception.Message); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Supplier conflict", "Supplier already exists."); }
    }

    [HttpPut("{supplierId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateSuppliers)]
    public async Task<IActionResult> Update(Guid orgId, Guid supplierId, UpdateSupplierRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = ThirdPartyRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid supplier", validation);
        try
        {
            var updated = await service.UpdateAsync(orgId, supplierId, request!, cancellationToken);
            return updated is null ? ProblemDetailsFactory.Create(404, "Supplier not found", "Supplier does not exist.") : Ok(new SupplierMutationResponse(updated.Id, "Proveedor actualizado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid supplier", exception.Message); }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Supplier conflict", "Supplier was modified by another request."); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Supplier conflict", "Supplier already exists."); }
    }

    [HttpPatch("{supplierId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteSuppliers)]
    public async Task<IActionResult> Deactivate(Guid orgId, Guid supplierId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        try
        {
            var result = await service.DeactivateAsync(orgId, supplierId, cancellationToken);
            return result is null ? ProblemDetailsFactory.Create(404, "Supplier not found", "Supplier does not exist.") : Ok(new SupplierMutationResponse(result.Id, "Estado del proveedor actualizado exitosamente."));
        }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Supplier conflict", "Supplier was modified by another request."); }
    }

    private IActionResult? EnsureTenant(Guid organizationId) => !tenantContext.IsAuthenticated
        ? ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.")
        : tenantContext.OrganizationId == organizationId ? null : ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");

    private new static class ProblemDetailsFactory
    {
        public static ObjectResult Create(int statusCode, string title, string detail) => HookRepair.Api.Security.ProblemDetailsFactory.Create(statusCode, title, detail);
    }
}
