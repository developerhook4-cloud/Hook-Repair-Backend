namespace HookRepair.Modules.Auth.Application.RefreshTokens;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenResult?> RotateAsync(
        string refreshToken,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

