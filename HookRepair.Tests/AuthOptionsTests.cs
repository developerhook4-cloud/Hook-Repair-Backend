using FluentAssertions;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Infrastructure.Auth;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Api.Controllers;
using HookRepair.Api.Security;

namespace HookRepair.Tests;
/*
public sealed class AuthOptionsTests
{
    [Fact]
    public void Accepts_secure_configuration()
    {
        var options = new AuthOptions
        {
            Issuer = "HookRepair.Api",
            Audience = "HookRepair.Web",
            SigningKey = "12345678901234567890123456789012",
            AllowedOrigins = ["https://localhost:4200"]
        };

        options.Validate();
    }

    [Fact]
    public void Rejects_short_signing_key()
    {
        var options = new AuthOptions
        {
            Issuer = "HookRepair.Api",
            Audience = "HookRepair.Web",
            SigningKey = "too-short"
        };

        Action validate = options.Validate;
        validate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Tenant_context_requires_organization()
    {
        var context = new TenantContext();

        Action set = () => context.Set(Guid.Empty);
        set.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Auth_options_implements_shared_configuration_contract()
    {
        typeof(AuthOptions).GetInterfaces().Should().Contain(typeof(IAuthOptions));

        foreach (var type in new[] { typeof(AuthService), typeof(AuthController), typeof(JwtTokenService), typeof(OriginValidationMiddleware) })
        {
            type.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => parameter.Name == "options")
                .Select(parameter => parameter.ParameterType)
                .Should().Contain(typeof(IAuthOptions));
        }
    }
}
*/