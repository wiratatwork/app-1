using App1.Backend.Configuration;
using App1.Backend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace App1.Backend.Services;

public sealed class BffSessionStore
{
    private readonly IMemoryCache _cache;
    private readonly BffSettings _settings;

    public BffSessionStore(IMemoryCache cache, Microsoft.Extensions.Options.IOptions<BffSettings> settings)
    {
        _cache = cache;
        _settings = settings.Value;
    }

    private static string SessionKey(string sessionId) => $"bff:session:{sessionId}";
    private static string PendingKey(string pendingId) => $"bff:pending:{pendingId}";

    public BffSessionData CreateSession(TokenResponse tokens, UserInfoDto user)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new BffSessionData
        {
            SessionId = sessionId,
            AccessToken = tokens.access_token,
            RefreshToken = tokens.refresh_token,
            IdToken = tokens.id_token,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokens.expires_in),
            LastSeenAt = DateTimeOffset.UtcNow,
            Subject = user.Subject,
            PreferredUsername = user.Username,
            Email = user.Email,
            Name = user.Name,
        };
        SaveSession(session);
        return session;
    }

    public void SaveSession(BffSessionData session)
    {
        session.LastSeenAt = DateTimeOffset.UtcNow;
        _cache.Set(SessionKey(session.SessionId), session, GetSessionLifetime());
    }

    public BffSessionData? GetSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;
        return _cache.TryGetValue(SessionKey(sessionId), out BffSessionData? session) ? session : null;
    }

    public void RemoveSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        _cache.Remove(SessionKey(sessionId));
    }

    public string SavePendingLogin(OidcPendingLogin pending)
    {
        var pendingId = Guid.NewGuid().ToString("N");
        _cache.Set(PendingKey(pendingId), pending, TimeSpan.FromMinutes(10));
        return pendingId;
    }

    public OidcPendingLogin? GetPendingLogin(string? pendingId)
    {
        if (string.IsNullOrWhiteSpace(pendingId)) return null;
        return _cache.TryGetValue(PendingKey(pendingId), out OidcPendingLogin? pending) ? pending : null;
    }

    public void RemovePendingLogin(string? pendingId)
    {
        if (string.IsNullOrWhiteSpace(pendingId)) return;
        _cache.Remove(PendingKey(pendingId));
    }

    private TimeSpan GetSessionLifetime() => TimeSpan.FromMinutes(Math.Max(5, _settings.SessionIdleMinutes));
}
