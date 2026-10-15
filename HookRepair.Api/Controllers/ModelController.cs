using HookRepair.Api.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/{orgId:guid}/models")]
[Authorize]
public sealed class ModelController(IModelService service, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewCatalogs)]
    public async Task<IActionResult> GetAll(Guid orgId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        return error ?? Ok(await service.GetAllAsync(orgId, cancellationToken));
    }

    [HttpGet("{modelId:guid}")]
    [Authorize(Policy = SystemPermissions.ViewCatalogs)]
    public async Task<IActionResult> GetById(Guid orgId, Guid modelId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var result = await service.GetByIdAsync(orgId, modelId, cancellationToken);
        return result is null ? ProblemDetailsFactory.Create(404, "Model not found", "Model does not exist.") : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateCatalogs)]
    public async Task<IActionResult> Create(Guid orgId, CreateModelRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = CatalogRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid model", validation);
        try
        {
            var created = await service.CreateAsync(orgId, request!, cancellationToken);
            return Ok(new CatalogMutationResponse(created.Id, "Modelo creado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid model", exception.Message); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Model conflict", "Model already exists for this brand."); }
    }

    [HttpPut("{modelId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateCatalogs)]
    public async Task<IActionResult> Update(Guid orgId, Guid modelId, UpdateModelRequest? request, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        var validation = CatalogRequestValidator.Validate(request);
        if (validation is not null) return ProblemDetailsFactory.Create(400, "Invalid model", validation);
        try
        {
            var updated = await service.UpdateAsync(orgId, modelId, request!, cancellationToken);
            return updated is null ? ProblemDetailsFactory.Create(404, "Model not found", "Model does not exist.") : Ok(new CatalogMutationResponse(updated.Id, "Modelo actualizado exitosamente."));
        }
        catch (ArgumentException exception) { return ProblemDetailsFactory.Create(400, "Invalid model", exception.Message); }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Model conflict", "Model was modified by another request."); }
        catch (DbUpdateException exception) when (DbUpdateConflict.IsUniqueViolation(exception)) { return ProblemDetailsFactory.Create(409, "Model conflict", "Model already exists for this brand."); }
    }

    [HttpPatch("{modelId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteCatalogs)]
    public async Task<IActionResult> Deactivate(Guid orgId, Guid modelId, CancellationToken cancellationToken)
    {
        var error = EnsureTenant(orgId);
        if (error is not null) return error;
        try
        {
            var result = await service.DeactivateAsync(orgId, modelId, cancellationToken);
            return result is null ? ProblemDetailsFactory.Create(404, "Model not found", "Model does not exist.") : Ok(new CatalogMutationResponse(result.Id, "Estado del modelo actualizado exitosamente."));
        }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsFactory.Create(409, "Model conflict", "Model was modified by another request."); }
    }

    private IActionResult? EnsureTenant(Guid organizationId) => !tenantContext.IsAuthenticated
        ? ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.")
        : tenantContext.OrganizationId == organizationId ? null : ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");

    private new static class ProblemDetailsFactory
    {
        public static ObjectResult Create(int statusCode, string title, string detail) => HookRepair.Api.Security.ProblemDetailsFactory.Create(statusCode, title, detail);
    }
}
