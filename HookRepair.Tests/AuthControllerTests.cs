using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using HookRepair.Api.Controllers;
using HookRepair.Modules.Auth.Application.Auth;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Tests;
/*
public sealed class AuthControllerTests
{
    [Fact]
    public async Task Refresh_without_cookie_returns_unauthorized()
    {
        var context = CreateContext();
        var controller = CreateController(context, new StubAuthService());

        var result = await controller.Refresh(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Refresh_rotates_cookie_and_returns_access_data_only()
    {
        var context = CreateContext("hook.refresh=old-refresh");
        var auth = new StubAuthService { RefreshResult = AuthTestData.Session() };
        var controller = CreateController(context, auth);

        var result = await controller.Refresh(CancellationToken.None);
        var response = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<FrontendAuthResponse>().Subject;
        var setCookie = context.Response.Headers.SetCookie.ToString();

        auth.RefreshedToken.Should().Be("old-refresh");
        response.Token.Should().Be("access-token");
        JsonSerializer.Serialize(response).Should().NotContain("RefreshToken");
        setCookie.Should().Contain("hook.refresh=").And.Contain("httponly");
        setCookie.Should().NotContain("hook.csrf=");
    }

    [Fact]
    public async Task Refresh_with_invalid_token_returns_unauthorized_and_deletes_cookie()
    {
        var context = CreateContext("hook.refresh=invalid-refresh");
        var controller = CreateController(context, new StubAuthService());

        var result = await controller.Refresh(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(401);
        context.Response.Headers.SetCookie.ToString().Should().Contain("hook.refresh=");
    }

    [Fact]
    public async Task Login_returns_access_data_and_secure_refresh_cookie()
    {
        var context = CreateContext(scheme: "https");
        var auth = new StubAuthService { LoginResult = AuthTestData.Session("login-refresh") };
        var controller = CreateController(context, auth);

        var result = await controller.Login(new("user@example.com", "correct-password"), CancellationToken.None);
        var response = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<FrontendAuthResponse>().Subject;
        var setCookie = context.Response.Headers.SetCookie.ToString();

        response.Token.Should().Be("access-token");
        setCookie.Should().Contain("hook.refresh=login-refresh").And.Contain("httponly").And.Contain("secure");
        setCookie.Should().NotContain("hook.csrf=");
    }

    [Fact]
    public async Task Login_returns_frontend_auth_contract()
    {
        var context = CreateContext(scheme: "https");
        var session = AuthTestData.Session("login-refresh");
        var controller = CreateController(context, new StubAuthService { LoginResult = session });

        var result = await controller.Login(new("user@example.com", "correct-password"), CancellationToken.None);
        var payload = JsonSerializer.SerializeToDocument(
            result.Should().BeOfType<OkObjectResult>().Which.Value,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var user = payload.RootElement.GetProperty("user");

        payload.RootElement.GetProperty("token").GetString().Should().Be("access-token");
        user.GetProperty("id").GetString().Should().Be(session.User.Id.ToString());
        user.GetProperty("name").GetString().Should().Be(session.User.Name);
        user.GetProperty("email").GetString().Should().Be(session.User.Email);
        user.GetProperty("roleId").GetString().Should().Be(session.Role.Id.ToString());
        user.GetProperty("organizationId").GetString().Should().Be(session.Organization.Id.ToString());
    }

    [Fact]
    public void Refresh_exposes_frontend_route_and_compatibility_alias()
    {
        var templates = typeof(AuthController)
            .GetMethod(nameof(AuthController.Refresh))!
            .GetCustomAttributes<HttpPostAttribute>()
            .Select(attribute => attribute.Template);

        templates.Should().Contain("refresh-token");
        templates.Should().Contain("refresh");
    }

    [Fact]
    public void Organization_ids_are_non_nullable_across_auth_contract()
    {
        typeof(User).GetProperty(nameof(User.OrganizationId))!.PropertyType.Should().Be(typeof(Guid));
        typeof(Role).GetProperty(nameof(Role.OrganizationId))!.PropertyType.Should().Be(typeof(Guid));
        typeof(AuthOrganizationDto).GetProperty(nameof(AuthOrganizationDto.Id))!.PropertyType.Should().Be(typeof(Guid));
        typeof(FrontendAuthUserDto).GetProperty(nameof(FrontendAuthUserDto.OrganizationId))!.PropertyType.Should().Be(typeof(Guid));
    }

    private static AuthController CreateController(DefaultHttpContext context, StubAuthService auth) =>
        new(auth, AuthTestData.Options, new TestHostEnvironment())
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

    private static DefaultHttpContext CreateContext(string? cookie = null, string scheme = "http")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        if (cookie is not null)
            context.Request.Headers.Cookie = cookie;
        return context;
    }
}
*/
