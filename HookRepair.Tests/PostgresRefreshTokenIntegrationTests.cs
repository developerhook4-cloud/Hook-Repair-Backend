using FluentAssertions;
using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Shared.Core.ValueObjects;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.RefreshTokens;
using HookRepair.Modules.Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class PostgresRefreshTokenIntegrationTests
{
    [Fact]
    public async Task Rotate_is_atomic_and_only_one_concurrent_request_succeeds()
    {
        var connection = Environment.GetEnvironmentVariable("HOOKREPAIR_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "HOOKREPAIR_TEST_CONNECTION is required for PostgreSQL integration tests.");
        var connectionString = connection!;

        var userId = Guid.NewGuid();
        await using (var seedDb = CreateDbContext(connectionString))
        {
            var (organization, role, user) = CreateUser(userId);
            seedDb.AddRange(organization, role, user);
            await seedDb.SaveChangesAsync();
        }

        var originalToken = await CreateRefreshToken(connectionString, userId);
        var first = Rotate(connectionString, originalToken.Token);
        var second = Rotate(connectionString, originalToken.Token);
        var results = await Task.WhenAll(first, second);

        results.Count(result => result is not null).Should().Be(1);
        results.Count(result => result is null).Should().Be(1);

        await using var verifyDb = CreateDbContext(connectionString);
        var storedTokens = await verifyDb.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId)
            .ToListAsync();
        storedTokens.Should().HaveCount(2);
        storedTokens.Single(token => token.TokenHash == originalToken.StoredHash).RevokedAt.Should().NotBeNull();
    }

    private static async Task<RefreshTokenResultWithHash> CreateRefreshToken(string connection, Guid userId)
    {
        await using var db = CreateDbContext(connection);
        var result = await new RefreshTokenService(db).CreateAsync(userId, TimeSpan.FromDays(7));
        var storedHash = (await db.RefreshTokens.IgnoreQueryFilters().SingleAsync(token => token.UserId == userId)).TokenHash;
        return new RefreshTokenResultWithHash(result.Token, storedHash);
    }

    private static async Task<HookRepair.Modules.Auth.Application.RefreshTokens.RefreshTokenResult?> Rotate(
        string connection,
        string token)
    {
        await using var db = CreateDbContext(connection);
        return await new RefreshTokenService(db).RotateAsync(token, TimeSpan.FromDays(7));
    }

    private static AuthDbContext CreateDbContext(string connection)
    {
        var tenant = new TenantContext();
        tenant.Set(Guid.NewGuid());
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AuthDbContext(options, tenant);
    }

    private static (Organization Organization, Role Role, User User) CreateUser(Guid userId)
    {
        var organization = new Organization
        {
            Name = "Integration Organization",
            Email = $"org-{userId:N}@example.com",
            Website = "https://example.com",
            Phone1 = "555-0100",
            Phone2 = "555-0101",
            Address = new FullAddress
            {
                Address = "1 Test Street",
                City = "Phoenix",
                State = "AZ",
                PostCode = "85001",
                Country = "US"
            }
        };
        var role = new Role
        {
            OrganizationId = organization.Id,
            Name = "integration",
            Organization = organization
        };
        var user = new User
        {
            Id = userId,
            OrganizationId = organization.Id,
            RoleId = role.Id,
            Name = "Integration User",
            Email = $"user-{userId:N}@example.com",
            PasswordHash = "hash",
            IsActive = true,
            Organization = organization,
            Role = role
        };
        return (organization, role, user);
    }

    private sealed record RefreshTokenResultWithHash(string Token, string StoredHash);
}
