using HookRepair.Modules.Auth.Application.Auth;
using HookRepair.Modules.Auth.Application.RefreshTokens;
using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthOptions _options;

    public AuthService(AuthDbContext dbContext, IPasswordService passwordService, IJwtTokenService jwtTokenService, IRefreshTokenService refreshTokenService, IAuthOptions options)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _options = options;
    }

    public async Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(item => item.Organization)
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || !user.IsActive || user.Role is null || !user.Role.Active || user.Organization is null || !user.Organization.Active)
            return null;

        if (!_passwordService.Verify(request.Password, user.PasswordHash))
            return null;

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var rotated = await _refreshTokenService.RotateAsync(refreshToken, TimeSpan.FromDays(_options.RefreshTokenDays), cancellationToken);
        if (rotated is null)
            return null;

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(item => item.Organization)
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == rotated.UserId, cancellationToken);

        if (user is null || !user.IsActive || user.Role is null || !user.Role.Active || user.Organization is null || !user.Organization.Active)
        {
            await _refreshTokenService.RevokeAsync(rotated.Token, cancellationToken);
            return null;
        }

        return BuildSession(user, rotated.Token, rotated.ExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _refreshTokenService.RevokeAsync(refreshToken, cancellationToken);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id, TimeSpan.FromDays(_options.RefreshTokenDays), cancellationToken);
        return BuildSession(user, refreshToken.Token, refreshToken.ExpiresAt);
    }

    private AuthSessionResult BuildSession(User user, string refreshToken, DateTime refreshExpiresAt)
    {
        var accessToken = _jwtTokenService.CreateAccessToken(user, user.Role);

        var userDto = new AuthUserDto(user.Id, user.Name, user.Email, user.RoleId, user.Role.Name, user.Role.Permissions, user.OrganizationId);

        return new AuthSessionResult(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshExpiresAt, userDto);
    }
}
