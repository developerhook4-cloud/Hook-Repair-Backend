namespace HookRepair.Modules.Auth.Application.Security;

public interface IAuthOptions
{
    string Issuer { get; }
    string Audience { get; }
    string SigningKey { get; }
    int AccessTokenMinutes { get; }
    int RefreshTokenDays { get; }
    string RefreshCookieName { get; }
    string[] AllowedOrigins { get; }
    void Validate();
}
