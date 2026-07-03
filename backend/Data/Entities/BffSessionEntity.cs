namespace App1.Backend.Data.Entities;

public sealed class BffSessionEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string? EncryptedRefreshToken { get; set; }
    public string? EncryptedIdToken { get; set; }
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public string? Subject { get; set; }
    public string? PreferredUsername { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}
