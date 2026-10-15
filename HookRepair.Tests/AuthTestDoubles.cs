using HookRepair.Modules.Auth.Application.Auth;
using HookRepair.Modules.Auth.Application.RefreshTokens;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HookRepair.Tests;
/*
internal static class AuthTestData
{
    public static AuthOptions Options => new()
    {
        Issuer = "HookRepair.Api",
        Audience = "HookRepair.Web",
        SigningKey = "12345678901234567890123456789012",
        RefreshTokenDays = 7,
        RefreshCookieName = "hook.refresh",
        AllowedOrigins = ["https://localhost:4200"]
    };

    public static AuthSessionResult Session(string refreshToken = "replacement-refresh") => new(
        "access-token",
        DateTime.UtcNow.AddMinutes(15),
        refreshToken,
        DateTime.UtcNow.AddDays(7),
        new AuthUserDto(Guid.NewGuid(), "Test User", "user@example.com"),
        new AuthOrganizationDto(Guid.NewGuid(), "Test Organization"),
        new AuthRoleDto(Guid.NewGuid(), "admin"),
        ["auth", "inventory"]);

    public static (Organization Organization, Role Role, User User) OrganizationUser(
        bool organizationActive = true,
        bool userActive = true)
    {
        var organization = new Organization
        {
            Name = "Test Organization",
            Email = "org@example.com",
            Website = "https://example.com",
            Phone1 = "555-0100",
            Phone2 = "555-0101",
            Active = organizationActive
        };
        var role = new Role
        {
            OrganizationId = organization.Id,
            Name = "admin",
            Organization = organization
        };
        role.Permissions = ["auth", "inventory"];
        var user = new User
        {
            OrganizationId = organization.Id,
            RoleId = role.Id,
            Name = "Test User",
            Email = "user@example.com",
            PasswordHash = "hash",
            IsActive = userActive,
            Organization = organization,
            Role = role
        };
        return (organization, role, user);
    }
}

internal sealed class StubPasswordService : IPasswordService
{
    public string Hash(string password) => "hash";

    public bool Verify(string password, string passwordHash) =>
        password == "correct-password" && passwordHash == "hash";
}

internal sealed class StubJwtTokenService : IJwtTokenService
{
    public AccessTokenResult CreateAccessToken(User user, Role role) =>
        new("access-token", DateTime.UtcNow.AddMinutes(15));
}

internal sealed class StubRefreshTokenService : IRefreshTokenService
{
    public RefreshTokenResult? RotationResult { get; set; }
    public List<string> RevokedTokens { get; } = [];

    public Task<RefreshTokenResult> CreateAsync(Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RefreshTokenResult(userId, "created-refresh", DateTime.UtcNow.Add(lifetime)));

    public Task<RefreshTokenResult?> RotateAsync(string refreshToken, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        Task.FromResult(RotationResult);

    public Task<bool> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RevokedTokens.Add(refreshToken);
        return Task.FromResult(true);
    }
}

internal sealed class StubAuthService : IAuthService
{
    public AuthSessionResult? LoginResult { get; set; }
    public AuthSessionResult? RefreshResult { get; set; }
    public string? RefreshedToken { get; private set; }

    public Task<AuthSessionResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(LoginResult);

    public Task<AuthSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RefreshedToken = refreshToken;
        return Task.FromResult(RefreshResult);
    }

    public Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MeResponse?>(null);
}

internal sealed class TestHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "HookRepair.Tests";
    public string EnvironmentName { get; set; } = Environments.Development;
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}*/
