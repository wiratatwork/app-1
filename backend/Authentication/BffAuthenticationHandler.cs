using System.Text.Encodings.Web;
using App1.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace App1.Backend.Authentication;

public sealed class BffAuthenticationOptions : AuthenticationSchemeOptions;

public sealed class BffAuthenticationHandler : AuthenticationHandler<BffAuthenticationOptions>
{
    private readonly BffCookieService _cookieService;
    private readonly BffSessionStore _sessionStore;
    private readonly BffTokenValidator _tokenValidator;
    private readonly KeycloakOidcService _oidcService;

    public BffAuthenticationHandler(
        IOptionsMonitor<BffAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        BffCookieService cookieService,
        BffSessionStore sessionStore,
        BffTokenValidator tokenValidator,
        KeycloakOidcService oidcService)
        : base(options, logger, encoder)
    {
        _cookieService = cookieService;
        _sessionStore = sessionStore;
        _tokenValidator = tokenValidator;
        _oidcService = oidcService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessionId = _cookieService.GetSessionId(Request);
        var session = _sessionStore.GetSession(sessionId);
        if (session is null)
        {
            return AuthenticateResult.NoResult();
        }

        var (principal, _) = await _tokenValidator.ValidateAccessTokenAsync(session.AccessToken, Context.RequestAborted);
        if (principal is not null)
        {
            _sessionStore.SaveSession(session);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        if (string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            _sessionStore.RemoveSession(sessionId);
            return AuthenticateResult.Fail("Session expired");
        }

        var refreshed = await _oidcService.RefreshTokenAsync(session.RefreshToken, Context.RequestAborted);
        if (refreshed is null)
        {
            _sessionStore.RemoveSession(sessionId);
            return AuthenticateResult.Fail("Unable to refresh session");
        }

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

        var (refreshedPrincipal, refreshedUser) = await _tokenValidator.ValidateAccessTokenAsync(session.AccessToken, Context.RequestAborted);
        if (refreshedPrincipal is null || refreshedUser is null)
        {
            _sessionStore.RemoveSession(sessionId);
            return AuthenticateResult.Fail("Refreshed token invalid");
        }

        session.Subject = refreshedUser.Subject;
        session.PreferredUsername = refreshedUser.Username;
        session.Email = refreshedUser.Email;
        session.Name = refreshedUser.Name;
        _sessionStore.SaveSession(session);

        return AuthenticateResult.Success(new AuthenticationTicket(refreshedPrincipal, Scheme.Name));
    }
}
