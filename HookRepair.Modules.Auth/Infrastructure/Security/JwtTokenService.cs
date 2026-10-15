using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace HookRepair.Modules.Auth.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IAuthOptions _options;

    public JwtTokenService(IAuthOptions options)
    {
        _options = options;
    }

    public AccessTokenResult CreateAccessToken(User user, Role role)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("role_id", role.Id.ToString()),
            new("role", role.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("org_id", user.OrganizationId.ToString())
        };

        if (role.Permissions is not null)
        {
            claims.AddRange(role.Permissions.Select(permission => new Claim("permission", permission)));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}