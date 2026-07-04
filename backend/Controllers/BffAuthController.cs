using App1.Backend.Configuration;
using App1.Backend.Models;
using App1.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace App1.Backend.Controllers;

[ApiController]
[Route("api/bff/auth")]
[AllowAnonymous]
public sealed class BffAuthController : ControllerBase
{
    private readonly BffSettings _bff;
    private readonly KeycloakOidcService _oidcService;
    private readonly BffSessionStore _sessionStore;
    private readonly BffCookieService _cookieService;
    private readonly BffTokenValidator _tokenValidator;

    public BffAuthController(
        IOptions<BffSettings> bff,
        KeycloakOidcService oidcService,
        BffSessionStore sessionStore,
        BffCookieService cookieService,
        BffTokenValidator tokenValidator)
    {
        _bff = bff.Value;
        _oidcService = oidcService;
        _sessionStore = sessionStore;
        _cookieService = cookieService;
        _tokenValidator = tokenValidator;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var (authUrl, pending) = _oidcService.BuildAuthorizationRequest();
        var pendingId = _sessionStore.SavePendingLogin(pending);
        _cookieService.SetPendingLoginCookie(Response, pendingId);
        return Redirect(authUrl);
    }

    [HttpGet("silent")]
    public IActionResult Silent()
    {
        if (!_bff.EnableSilentSso)
        {
            return BadRequest(new ApiErrorResponse { Message = "Silent SSO is disabled for this application." });
        }

        var (authUrl, pending) = _oidcService.BuildAuthorizationRequest("none");
        var pendingId = _sessionStore.SavePendingLogin(pending);
        _cookieService.SetPendingLoginCookie(Response, pendingId);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var pendingId = _cookieService.GetPendingLoginId(Request); // ดึงค่า Cookie ที่ชื่อ app1_bff_pending
        var pending = _sessionStore.GetPendingLogin(pendingId); // ดึงข้อมูล Pending Login จากฐานข้อมูล
        _cookieService.ClearPendingLoginCookie(Response); // ลบค่า Cookie ที่ชื่อ app1_bff_pending

        if (!string.IsNullOrWhiteSpace(error))
        {
            if (string.Equals(error, "login_required", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}?sso=failed");
            }

            return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}?error={Uri.EscapeDataString(error)}");
        }  // ถ้ามี error ให้ Login ใหม่

        if (pending is null || string.IsNullOrWhiteSpace(code) || !string.Equals(state, pending.State, StringComparison.Ordinal))
        {
            return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}?error=invalid_callback");
        } // ถ้าไม่พบ Pending Login หรือ code หรือ state ไม่ตรงกับ DB State ให้ Login ใหม่

        _sessionStore.RemovePendingLogin(pendingId); // ลบข้อมูล Pending Login จากฐานข้อมูล

        var tokens = await _oidcService.ExchangeCodeAsync(code, pending.CodeVerifier, cancellationToken);
        if (tokens is null)
        {
            return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}?error=token_exchange_failed");
        }

        var (_, user) = await _tokenValidator.ValidateAccessTokenAsync(tokens.access_token, cancellationToken);
        if (user is null)
        {
            return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}?error=token_validation_failed");
        }

        var session = _sessionStore.CreateSession(tokens, user); // เข้ารหัส บันทึก Session Token
        _cookieService.SetSessionCookie(Response, session.SessionId); // ตั้งค่า Cookie ที่ชื่อ app1_bff_session

        return Redirect($"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLoginPath}"); // ไปหน้า HomeComponent
    }

    [HttpGet("session")]
    public async Task<ActionResult<SessionStatusResponse>> Session(CancellationToken cancellationToken)
    {
        var sessionId = _cookieService.GetSessionId(Request); // ดึงค่า Cookie ที่ชื่อ app1_bff_session
        var session = _sessionStore.GetSession(sessionId); // ดึงข้อมูล Session จากฐานข้อมูล
        if (session is null) // ถ้าไม่พบ Session ให้ส่งกลับค่า false
        {
            return Ok(new SessionStatusResponse { Authenticated = false });
        }

        var (_, user) = await _tokenValidator.ValidateAccessTokenAsync(session.AccessToken, cancellationToken);
        if (user is null && !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            var refreshed = await _oidcService.RefreshTokenAsync(session.RefreshToken, cancellationToken);
            if (refreshed is not null)
            {
                session.AccessToken = refreshed.access_token;
                if (!string.IsNullOrWhiteSpace(refreshed.refresh_token))
                {
                    session.RefreshToken = refreshed.refresh_token;
                }

                if (!string.IsNullOrWhiteSpace(refreshed.id_token))
                {
                    session.IdToken = refreshed.id_token;
                }

                session.AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshed.expires_in);
                (_, user) = await _tokenValidator.ValidateAccessTokenAsync(session.AccessToken, cancellationToken);
                if (user is not null)
                {
                    _sessionStore.SaveSession(session);
                }
            }
        }

        if (user is null)
        {
            _sessionStore.RemoveSession(sessionId);
            _cookieService.ClearSessionCookie(Response);
            return Ok(new SessionStatusResponse { Authenticated = false });
        }

        return Ok(new SessionStatusResponse
        {
            Authenticated = true,
            User = user,
        });
    }

    [HttpPost("logout")]
    public IActionResult LogoutPost()
    {
        return LogoutInternal();
    }

    [HttpGet("logout")]
    public IActionResult LogoutGet()
    {
        return LogoutInternal();
    }

    private IActionResult LogoutInternal()
    {
        var sessionId = _cookieService.GetSessionId(Request);
        var session = _sessionStore.GetSession(sessionId);
        var idTokenHint = session?.IdToken;

        _sessionStore.RemoveSession(sessionId);
        _cookieService.ClearSessionCookie(Response);

        var logoutUrl = _oidcService.BuildLogoutUrl(idTokenHint);
        return Redirect(logoutUrl);
    }
}
