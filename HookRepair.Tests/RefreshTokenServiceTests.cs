using FluentAssertions;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class RefreshTokenServiceTests
{
    [Fact]
    public async Task Create_persists_only_sha256_hash_and_returns_plaintext_once()
    {
        await using var db = CreateDbContext();
        var service = new RefreshTokenService(db);

        var result = await service.CreateAsync(Guid.NewGuid(), TimeSpan.FromDays(7));
        var stored = await db.RefreshTokens.IgnoreQueryFilters().SingleAsync();

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Token.Should().HaveLength(86);
        result.Token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        stored.TokenHash.Should().HaveLength(64);
        stored.TokenHash.Should().MatchRegex("^[A-F0-9]+$");
        stored.TokenHash.Should().NotBe(result.Token);
        stored.ExpiresAt.Should().BeCloseTo(result.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Revoke_marks_matching_token_and_rejects_second_revoke()
    {
        await using var db = CreateDbContext();
        var service = new RefreshTokenService(db);
        var created = await service.CreateAsync(Guid.NewGuid(), TimeSpan.FromDays(7));

        var firstRevoke = await service.RevokeAsync(created.Token);
        var secondRevoke = await service.RevokeAsync(created.Token);

        firstRevoke.Should().BeTrue();
        secondRevoke.Should().BeFalse();
        (await db.RefreshTokens.IgnoreQueryFilters().SingleAsync()).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Revoke_returns_false_for_empty_or_unknown_token()
    {
        await using var db = CreateDbContext();
        var service = new RefreshTokenService(db);

        (await service.RevokeAsync(" ")).Should().BeFalse();
        (await service.RevokeAsync("unknown-token")).Should().BeFalse();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options, new HookRepair.Modules.Auth.Infrastructure.Security.TenantContext());
    }
}
