using HookRepair.Api.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/brands")]
[Authorize]
public sealed class BrandController(IBrandService service, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewCatalogs)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        return error ?? Ok(await service.GetAllAsync(orgId, cancellationToken));
    }

    [HttpGet("{brandId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewCatalogs)]
    public async Task<IActionResult> GetById(Guid orgId, Guid brandId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var result = await service.GetByIdAsync(orgId, brandId, cancellationToken);
        return result is null ? ProblemDetailsFactory.Create(404, "Brand not found", "Brand does not exist.") : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateCatalogs)]
    public async Task<IActionResult> Create(Guid orgId, CreateBrandRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = CatalogRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid brand", validation);
        try
        {
            var created = await service.CreateAsync(orgId, request!, cancellationToken);
            return Ok(new CatalogMutationResponse(created.Id, "Marca creada exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid brand", exception.Message); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Brand conflict", "Brand already exists."); }
    }

    [HttpPut("{brandId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateCatalogs)]
    public async Task<IActionResult> Update(Guid orgId, Guid brandId, UpdateBrandRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = CatalogRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid brand", validation);
        try
        {
            var updated = await service.UpdateAsync(orgId, brandId, request!, cancellationToken);
            return updated is null ? ProblemDetailsFactory.Create(404, "Brand not found", "Brand does not exist.") : Ok(new CatalogMutationResponse(updated.Id, "Marca actualizada exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid brand", exception.Message); }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Brand conflict", "Brand was modified by another request."); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Brand conflict", "Brand already exists."); }
    }

    [HttpPatch("{brandId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteCatalogs)]
    public async Task<IActionResult> Deactivate(Guid orgId, Guid brandId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        try
        {
            var result = await service.DeactivateAsync(orgId, brandId, cancellationToken);
            return result is null ? ProblemDetailsFactory.Create(404, "Brand not found", "Brand does not exist.") : Ok(new CatalogMutationResponse(result.Id, "Estado de la marca actualizado exitosamente."));
        }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Brand conflict", "Brand was modified by another request."); }
    }

    private IActionResult? EnsureTenant(Guid organizationId) => !tenantContext.IsAuthenticated
        ? ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.")
        : tenantContext.OrganizationId == organizationId ? null : ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");

    private new static class ProblemDetailsFactory
    {
        public static ObjectResult Create(int statusCode, string title, string detail) => HookRepair.Api.Security.ProblemDetailsFactory.Create(statusCode, title, detail);
    }
}
