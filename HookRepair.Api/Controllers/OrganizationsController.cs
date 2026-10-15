using HookRepair.Api.Models.Organizations;
using HookRepair.Modules.Auth.Application.Organizations;
using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Shared.Core.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IDefaultRoleService _defaultRoleService;
    private readonly IRoleManagementService _roleManagementService;

    public OrganizationsController(
        IOrganizationService organizationService,
        IDefaultRoleService defaultRoleService,
        IRoleManagementService roleManagementService)
    {
        _organizationService = organizationService;
        _defaultRoleService = defaultRoleService;
        _roleManagementService = roleManagementService;
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.ViewOrganizations)]
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);

        return Ok(organizations.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = SystemPermissions.ViewOrganizations)]
    public async Task<ActionResult<OrganizationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var organization = organizations.SingleOrDefault(item => item.Id == id);

        return organization is null
            ? NotFound()
            : Ok(ToResponse(organization));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.UpdateOrganizations)]
    public async Task<ActionResult<OrganizationResponse>> Create(
        SaveOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationService.CreateOrganizationAsync(
            ToEntity(request),
            cancellationToken);

        await _defaultRoleService.RegisterAsync(organization.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = organization.Id },
            ToResponse(organization));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateOrganizations)]
    public async Task<ActionResult<OrganizationResponse>> Update(
        Guid id,
        SaveOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == id);

        if (!exists)
        {
            return NotFound();
        }

        var updatedOrganization = await _organizationService.UpdateOrganizationAsync(
            ToEntity(request, id),
            cancellationToken);

        return Ok(ToResponse(updatedOrganization));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteOrganizations)]
    public async Task<ActionResult<OrganizationResponse>> UpdateStatus(
        Guid id,
        bool active,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == id);
        if (!exists)
        {
            return NotFound();
        }
        var updatedOrganization = await _organizationService.UpdateOrganizationStatusAsync(
            id,
            active,
            cancellationToken);
        return Ok(ToResponse(updatedOrganization));
    }

    [HttpPost("{id:guid}/users")]
    [Authorize(Policy = SystemPermissions.UpdateOrganizations)]
    public async Task<ActionResult> AddUserToOrganization(
        Guid id,
        AddUserToOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == id);
        if (!exists)
        {
            return NotFound();
        }
        User newUser = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = id,
            RoleId = request.RoleId,
            Name = request.Name,
            Email = request.Email,
            IsActive = true
        };
        await _organizationService.CreateOrganizationUserAsync(
            newUser,
            request.Password,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/users")]
    [Authorize(Policy = SystemPermissions.ViewOrganizations)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsersByOrganization(
        Guid id,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == id);
        if (!exists)
        {
            return NotFound();
        }
        var users = await _organizationService.GetAllOrganizationUsers(id, cancellationToken);
        return Ok(users.Select(user => new UserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.Name,
            user.IsActive)));
    }

    [HttpPut("{organizationId:guid}/users/{userId:guid}")]
    [Authorize(Policy = SystemPermissions.UpdateOrganizations)]
    public async Task<ActionResult> UpdateUserInOrganization(
        Guid organizationId,
        Guid userId,
        UpdateUserInOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == organizationId);
        if (!exists)
        {
            return NotFound();
        }
        var users = await _organizationService.GetAllOrganizationUsers(organizationId, cancellationToken);
        var userExists = users.Any(user => user.Id == userId);
        if (!userExists)
        {
            return NotFound();
        }
        User updatedUser = new()
        {
            Id = userId,
            OrganizationId = organizationId,
            RoleId = request.RoleId,
            Name = request.Name,
            Email = request.Email
        };
        await _organizationService.UpdateOrganizationUserAsync(
            updatedUser,
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{organizationId:guid}/users/{userId:guid}/status")]
    [Authorize(Policy = SystemPermissions.DeleteOrganizations)]
    public async Task<ActionResult> UpdateUserStatusInOrganization(
        Guid organizationId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var exists = organizations.Any(item => item.Id == organizationId);
        if (!exists)
        {
            return NotFound();
        }
        var users = await _organizationService.GetAllOrganizationUsers(organizationId, cancellationToken);
        var userExists = users.Any(user => user.Id == userId);
        if (!userExists)
        {
            return NotFound();
        }
        await _organizationService.UpdateOrganizationUserStatusAsync(
            userId,
            isActive,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<OrganizationResponse>> GetMyOrganization(CancellationToken cancellationToken)
    {
        var orgIdClaim = User.FindFirst("org_id");
        if (orgIdClaim is null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
            return Unauthorized();

        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        var organization = organizations.SingleOrDefault(item => item.Id == orgId);

        return organization is null ? NotFound() : Ok(ToResponse(organization));
    }

    [HttpPut("mine")]
    public async Task<ActionResult<OrganizationResponse>> UpdateMyOrganization(
        SaveOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var orgIdClaim = User.FindFirst("org_id");
        if (orgIdClaim is null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
            return Unauthorized();

        var organizations = await _organizationService.GetAllOrganizations(cancellationToken);
        if (!organizations.Any(item => item.Id == orgId))
            return NotFound();

        var updatedOrganization = await _organizationService.UpdateOrganizationAsync(
            ToEntity(request, orgId),
            cancellationToken);

        return Ok(ToResponse(updatedOrganization));
    }

    private static Organization ToEntity(
        SaveOrganizationRequest request,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Website = request.Website,
            Active = request.Active,
            Phone1 = request.Phone1,
            Phone2 = request.Phone2,
            Address = new FullAddress
            {
                Address = request.Address.Address,
                City = request.Address.City,
                State = request.Address.State,
                PostCode = request.Address.PostCode,
                Country = request.Address.Country,
            },
        };

    private static OrganizationResponse ToResponse(Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            organization.Email,
            organization.Website ?? string.Empty,
            organization.Active,
            organization.Phone1 ?? string.Empty,
            organization.Phone2 ?? string.Empty,
           organization.Address is null
            ? new AddressResponse("","","","","")
            : new AddressResponse(
                organization.Address.Address ?? string.Empty,
                organization.Address.City ?? string.Empty,
                organization.Address.State ?? string.Empty,
                organization.Address.PostCode ?? string.Empty,
                organization.Address.Country ?? string.Empty));
}