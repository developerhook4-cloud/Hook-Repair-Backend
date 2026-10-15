using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Application.Users;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HookRepair.Api.Controllers
{
    [ApiController]
    [Route("api/{orgId:guid}/users")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITenantContext _tenantContext;

        public UserController(IUserService userService, ITenantContext tenantContext)
        {
            _userService = userService;
            _tenantContext = tenantContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponse>>> Get(Guid orgId, CancellationToken cancellationToken)
        {
            var tenantError = EnsureTenant(orgId);
            if (tenantError is not null) return new ObjectResult(tenantError) { StatusCode = (tenantError as ObjectResult)?.StatusCode ?? 403 };

            var users = await _userService.GetUsersAsync(cancellationToken);
            return Ok(users.Select(ToResponse));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid orgId, Guid id, CancellationToken cancellationToken)
        {
            var tenantError = EnsureTenant(orgId);
            if (tenantError is not null) return new ObjectResult(tenantError) { StatusCode = (tenantError as ObjectResult)?.StatusCode ?? 403 };

            var users = await _userService.GetUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Id == id && u.OrganizationId == orgId);

            if (user is null) return NotFound();
            return Ok(ToResponse(user));
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid orgId, SaveUserRequest request, CancellationToken cancellationToken)
        {
            var tenantError = EnsureTenant(orgId);
            if (tenantError is not null) return tenantError;

            var entity = ToEntity(request, orgId);

            var user = await _userService.CreateUserAsync(entity, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { orgId, id = user.Id }, ToResponse(user));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid orgId, Guid id, SaveUserRequest request, CancellationToken cancellationToken)
        {
            var tenantError = EnsureTenant(orgId);
            if (tenantError is not null) return tenantError;

            var entity = ToEntity(request, orgId, id);
            var updatedUser = await _userService.UpdateUserAsync(entity, id, request.Password, cancellationToken);

            return Ok(ToResponse(updatedUser));
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid orgId, Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
        {
            var tenantError = EnsureTenant(orgId);
            if (tenantError is not null) return tenantError;

            await _userService.UpdateUserStatusAsync(id, request.Active, cancellationToken);
            return NoContent();
        }

        [HttpPut("~/api/users/me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var (success, errorMessage) = await _userService.UpdateProfileAsync(userId, request, cancellationToken);

            if (!success)
                return BadRequest(new { message = errorMessage });

            return Ok(new { message = "Perfil actualizado exitosamente." });
        }

        private static User ToEntity(SaveUserRequest request, Guid orgId, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            RoleId = request.RoleId,
            IsActive = request.Active,
            OrganizationId = orgId
        };

        private static UserResponse ToResponse(User user) =>
            new(
                user.Id,
                user.Name,
                user.Email,
                user.Role?.Name ?? "Sin Rol",
                user.RoleId,
                user.IsActive);

        private IActionResult? EnsureTenant(Guid organizationId)
        {
            if (!_tenantContext.IsAuthenticated)
                return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "A valid authenticated session is required.");

            return _tenantContext.OrganizationId == organizationId
                ? null
                : HookRepair.Api.Security.ProblemDetailsFactory.Create(403, "Forbidden", "User does not belong to this organization.");
        }
    }

    public record UpdateUserStatusRequest(bool Active);
}