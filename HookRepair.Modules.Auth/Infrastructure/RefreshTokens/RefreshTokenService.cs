using System.Security.Cryptography;
using System.Text;
using HookRepair.Modules.Auth.Application.RefreshTokens;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure.RefreshTokens;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenSizeInBytes = 64;
    private readonly AuthDbContext _dbContext;

    public RefreshTokenService(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        ValidateLifetime(lifetime);

        var now = DateTime.UtcNow;
        var tokenValue = CreateTokenValue();
        var expiresAt = now.Add(lifetime);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = now,
            ExpiresAt = expiresAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new RefreshTokenResult(userId, tokenValue, expiresAt);
    }

    public async Task<RefreshTokenResult?> RotateAsync(
        string refreshToken,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        ValidateLifetime(lifetime);
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tokenHash = HashToken(refreshToken);
        var tokenId = await _dbContext.Database
            .SqlQueryRaw<Guid>(
                "SELECT id AS \"Value\" FROM refresh_tokens WHERE token_hash = {0} FOR UPDATE",
                tokenHash)
            .SingleOrDefaultAsync(cancellationToken);

        if (tokenId == Guid.Empty)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var storedToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .SingleAsync(token => token.Id == tokenId, cancellationToken);
        var now = DateTime.UtcNow;

        if (storedToken.RevokedAt is not null || storedToken.ExpiresAt <= now)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var owner = await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(user => user.Organization)
            .SingleOrDefaultAsync(user => user.Id == storedToken.UserId, cancellationToken);

        if (owner is null || !owner.IsActive || owner.Organization is null || !owner.Organization.Active)
        {
            storedToken.RevokedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        storedToken.RevokedAt = now;
        var newTokenValue = CreateTokenValue();
        var newExpiration = now.Add(lifetime);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = HashToken(newTokenValue),
            CreatedAt = now,
            ExpiresAt = newExpiration
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RefreshTokenResult(storedToken.UserId, newTokenValue, newExpiration);
    }

    public async Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
            return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string CreateTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeInBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static void ValidateLifetime(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Refresh token lifetime must be greater than zero.");
    }
}
