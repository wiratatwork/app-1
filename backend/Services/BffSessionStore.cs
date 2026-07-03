using App1.Backend.Configuration;
using App1.Backend.Data;
using App1.Backend.Data.Entities;
using App1.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace App1.Backend.Services;

public sealed class BffSessionStore
{
    private static readonly TimeSpan PendingLoginLifetime = TimeSpan.FromMinutes(10);

    private readonly IDbContextFactory<BffDbContext> _dbFactory;
    private readonly BffSettings _settings;
    private readonly TokenCipherService _cipher;
    private readonly ILogger<BffSessionStore> _logger;

    public BffSessionStore(
        IDbContextFactory<BffDbContext> dbFactory,
        IOptions<BffSettings> settings,
        TokenCipherService cipher,
        ILogger<BffSessionStore> logger)
    {
        _dbFactory = dbFactory;
        _settings = settings.Value;
        _cipher = cipher;
        _logger = logger;
    }

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
        using var db = _dbFactory.CreateDbContext();
        var entity = db.Sessions.Find(session.SessionId);
        if (entity is null)
        {
            entity = new BffSessionEntity { SessionId = session.SessionId };
            db.Sessions.Add(entity);
        }

        MapSessionToEntity(session, entity);
        db.SaveChanges();
    }

    public BffSessionData? GetSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        using var db = _dbFactory.CreateDbContext();
        var entity = db.Sessions.AsNoTracking().FirstOrDefault(s => s.SessionId == sessionId);
        if (entity is null)
        {
            return null;
        }

        if (IsSessionExpired(entity.LastSeenAt))
        {
            db.Sessions.Where(s => s.SessionId == sessionId).ExecuteDelete();
            return null;
        }

        try
        {
            return MapEntityToSession(entity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt session {SessionId}; removing invalid session.", sessionId);
            db.Sessions.Where(s => s.SessionId == sessionId).ExecuteDelete();
            return null;
        }
    }

    public void RemoveSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        using var db = _dbFactory.CreateDbContext();
        db.Sessions.Where(s => s.SessionId == sessionId).ExecuteDelete();
    }

    public string SavePendingLogin(OidcPendingLogin pending)
    {
        var pendingId = Guid.NewGuid().ToString("N");
        using var db = _dbFactory.CreateDbContext();
        db.PendingLogins.Add(new BffPendingLoginEntity
        {
            PendingId = pendingId,
            State = pending.State,
            CodeVerifier = pending.CodeVerifier,
            Prompt = pending.Prompt,
            CreatedAt = pending.CreatedAt,
        });
        db.SaveChanges();
        return pendingId;
    }

    public OidcPendingLogin? GetPendingLogin(string? pendingId)
    {
        if (string.IsNullOrWhiteSpace(pendingId))
        {
            return null;
        }

        using var db = _dbFactory.CreateDbContext();
        var entity = db.PendingLogins.AsNoTracking().FirstOrDefault(p => p.PendingId == pendingId);
        if (entity is null)
        {
            return null;
        }

        if (IsPendingLoginExpired(entity.CreatedAt))
        {
            db.PendingLogins.Where(p => p.PendingId == pendingId).ExecuteDelete();
            return null;
        }

        return new OidcPendingLogin
        {
            State = entity.State,
            CodeVerifier = entity.CodeVerifier,
            Prompt = entity.Prompt,
            CreatedAt = entity.CreatedAt,
        };
    }

    public void RemovePendingLogin(string? pendingId)
    {
        if (string.IsNullOrWhiteSpace(pendingId))
        {
            return;
        }

        using var db = _dbFactory.CreateDbContext();
        db.PendingLogins.Where(p => p.PendingId == pendingId).ExecuteDelete();
    }

    private void MapSessionToEntity(BffSessionData session, BffSessionEntity entity)
    {
        entity.EncryptedAccessToken = _cipher.Encrypt(session.AccessToken);
        entity.EncryptedRefreshToken = _cipher.EncryptOptional(session.RefreshToken);
        entity.EncryptedIdToken = _cipher.EncryptOptional(session.IdToken);
        entity.AccessTokenExpiresAt = session.AccessTokenExpiresAt;
        entity.LastSeenAt = session.LastSeenAt;
        entity.Subject = session.Subject;
        entity.PreferredUsername = session.PreferredUsername;
        entity.Email = session.Email;
        entity.Name = session.Name;
    }

    private BffSessionData MapEntityToSession(BffSessionEntity entity) => new()
    {
        SessionId = entity.SessionId,
        AccessToken = _cipher.Decrypt(entity.EncryptedAccessToken),
        RefreshToken = _cipher.DecryptOptional(entity.EncryptedRefreshToken),
        IdToken = _cipher.DecryptOptional(entity.EncryptedIdToken),
        AccessTokenExpiresAt = entity.AccessTokenExpiresAt,
        LastSeenAt = entity.LastSeenAt,
        Subject = entity.Subject,
        PreferredUsername = entity.PreferredUsername,
        Email = entity.Email,
        Name = entity.Name,
    };

    private bool IsSessionExpired(DateTimeOffset lastSeenAt) =>
        DateTimeOffset.UtcNow - lastSeenAt > GetSessionLifetime();

    private bool IsPendingLoginExpired(DateTimeOffset createdAt) =>
        DateTimeOffset.UtcNow - createdAt > PendingLoginLifetime;

    private TimeSpan GetSessionLifetime() => TimeSpan.FromMinutes(Math.Max(5, _settings.SessionIdleMinutes));
}
