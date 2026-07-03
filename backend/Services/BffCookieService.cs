using App1.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace App1.Backend.Services;

public sealed class BffCookieService
{
    private readonly BffSettings _settings;
    private readonly IWebHostEnvironment _environment;

    public BffCookieService(IOptions<BffSettings> settings, IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _environment = environment;
    }

    public string SessionCookieName => _settings.SessionCookieName;
    public string PendingLoginCookieName => _settings.PendingLoginCookieName;

    public void SetSessionCookie(HttpResponse response, string sessionId)
    {
        response.Cookies.Append(SessionCookieName, sessionId, BuildCookieOptions(DateTimeOffset.UtcNow.AddMinutes(_settings.SessionIdleMinutes)));
    }

    public void ClearSessionCookie(HttpResponse response)
    {
        response.Cookies.Delete(SessionCookieName, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
    }

    public string? GetSessionId(HttpRequest request) => request.Cookies[SessionCookieName];

    public void SetPendingLoginCookie(HttpResponse response, string pendingId)
    {
        response.Cookies.Append(PendingLoginCookieName, pendingId, BuildCookieOptions(DateTimeOffset.UtcNow.AddMinutes(10)));
    } // ตั้งค่า Cookie ที่ชื่อ app1_bff_pending ด้วยค่า pendingId และอายุ 10 นาที

    public void ClearPendingLoginCookie(HttpResponse response)
    {
        response.Cookies.Delete(PendingLoginCookieName, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
    }

    public string? GetPendingLoginId(HttpRequest request) => request.Cookies[PendingLoginCookieName];

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        var secure = !_environment.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires,
        };
    }
}
