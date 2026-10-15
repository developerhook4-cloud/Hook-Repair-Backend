using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HookRepair.Api.Security;
using HookRepair.Modules.Auth.Application.Auth;
using HookRepair.Modules.Auth.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HookRepair.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthOptions _options;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IAuthOptions options, IWebHostEnvironment environment)
    {
        _authService = authService;
        _options = options;
        _environment = environment;
    }

    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await _authService.LoginAsync(request, cancellationToken);
        if (session is null)
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Invalid credentials", "Email or password is invalid.");

        SetSessionCookies(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToFrontendResponse(session));
    }

    [AllowAnonymous, HttpPost("refresh-token"), EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_options.RefreshCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "Authentication is required.");

        var session = await _authService.RefreshAsync(refreshToken, cancellationToken);
        if (session is null)
        {
            Response.Cookies.Delete(_options.RefreshCookieName, CookieOptions());
            return HookRepair.Api.Security.ProblemDetailsFactory.Create(401, "Unauthorized", "Authentication is required.");
        }

        SetSessionCookies(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToFrontendResponse(session));
    }

    [AllowAnonymous, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(Request.Cookies[_options.RefreshCookieName], cancellationToken);
        Response.Cookies.Delete(_options.RefreshCookieName, CookieOptions());

        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    private void SetSessionCookies(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(_options.RefreshCookieName, refreshToken, CookieOptions(expiresAt, httpOnly: true));
    }

    private static AuthResponse ToFrontendResponse(AuthSessionResult session) =>
     new(session.AccessToken, session.User);

    private CookieOptions CookieOptions(DateTime? expiresAt = null, bool httpOnly = true)
    {
        var secure = !_environment.IsDevelopment() || Request.IsHttps;
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = secure,
            // Frontend y backend viven en dominios distintos: la cookie debe poder cruzar sitios.
            // SameSite=None exige Secure; en desarrollo sin HTTPS se usa Lax.
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/api/auth"
        };
    }
}