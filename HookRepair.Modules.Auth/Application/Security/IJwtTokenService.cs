using HookRepair.Modules.Auth.Core.Entities;

namespace HookRepair.Modules.Auth.Application.Security;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user, Role role);
}
