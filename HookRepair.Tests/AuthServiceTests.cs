using FluentAssertions;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;
/*
public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_normalizes_email_and_returns_identity_permissions()
    {
        await using var db = CreateDbContext();
        var (_, role, user) = AuthTestData.OrganizationUser();
        db.Add(user);
        await db.SaveChangesAsync();
        var refreshTokens = new StubRefreshTokenService();
        var service = CreateService(db, refreshTokens);

        var result = await service.LoginAsync(new(" USER@EXAMPLE.COM ", "correct-password"));

        result.Should().NotBeNull();
        result!.User.Email.Should().Be("user@example.com");
        result.Role.Name.Should().Be(role.Name);
        result.Permissions.Should().BeEquivalentTo(["Inventory.View", "Users.View"]);
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("created-refresh");
    }

    [Fact]
    public async Task Login_returns_null_for_wrong_password()
    {
        await using var db = CreateDbContext();
        var (_, _, user) = AuthTestData.OrganizationUser();
        db.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, new StubRefreshTokenService());

        var result = await service.LoginAsync(new("user@example.com", "wrong-password"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_returns_null_for_inactive_user_or_organization()
    {
        foreach (var inactiveOrganization in new[] { false, true })
        {
            await using var db = CreateDbContext();
            var (_, _, user) = AuthTestData.OrganizationUser(
                organizationActive: !inactiveOrganization,
                userActive: inactiveOrganization);
            db.Add(user);
            await db.SaveChangesAsync();
            var service = CreateService(db, new StubRefreshTokenService());

            var result = await service.LoginAsync(new("user@example.com", "correct-password"));

            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task Refresh_returns_null_and_revokes_replacement_when_owner_is_inactive()
    {
        await using var db = CreateDbContext();
        var (_, _, user) = AuthTestData.OrganizationUser(userActive: false);
        db.Add(user);
        await db.SaveChangesAsync();
        var refreshTokens = new StubRefreshTokenService
        {
            RotationResult = new(user.Id, "replacement-refresh", DateTime.UtcNow.AddDays(7))
        };
        var service = CreateService(db, refreshTokens);

        var result = await service.RefreshAsync("old-refresh");

        result.Should().BeNull();
        refreshTokens.RevokedTokens.Should().ContainSingle().Which.Should().Be("replacement-refresh");
    }

    private static AuthService CreateService(AuthDbContext db, StubRefreshTokenService refreshTokens) =>
        new(db, new StubPasswordService(), new StubJwtTokenService(), refreshTokens, AuthTestData.Options);

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options, new HookRepair.Modules.Auth.Infrastructure.Security.TenantContext());
    }
}
*/