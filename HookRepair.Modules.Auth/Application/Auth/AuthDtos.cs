using System.ComponentModel.DataAnnotations;

namespace HookRepair.Modules.Auth.Application.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password);
public sealed record AuthUserDto(
    Guid Id, 
    string Name,
    string Email,
    Guid RoleId,
    string Role,
    string[] Permissions, 
    Guid OrganizationId);
public sealed record AuthResponse(string Token, AuthUserDto User);
public sealed record AuthSessionResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    AuthUserDto User);
public interface IAuthService
{
    Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
}