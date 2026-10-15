namespace HookRepair.Modules.Auth.Application.RefreshTokens;

/// <summary>
/// Datos que se devuelven una sola vez al emitir o rotar un refresh token.
/// En la base de datos se guardará únicamente el hash del token.
/// </summary>
public sealed record RefreshTokenResult(Guid UserId, string Token, DateTime ExpiresAt);
