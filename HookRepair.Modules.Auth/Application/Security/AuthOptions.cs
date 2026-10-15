namespace HookRepair.Modules.Auth.Application.Security;

public sealed class AuthOptions : IAuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
    public string RefreshCookieName { get; init; } = "hook.refresh";
    public string[] AllowedOrigins { get; init; } = [];

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Auth issuer and audience are required.");

        if (SigningKey.Length < 32)
            throw new InvalidOperationException("Auth signing key must contain at least 32 characters.");

        if (AccessTokenMinutes <= 0 || RefreshTokenDays <= 0)
            throw new InvalidOperationException("Auth token lifetimes must be greater than zero.");

        if (AllowedOrigins.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Auth allowed origins cannot contain empty values.");
    }
}
