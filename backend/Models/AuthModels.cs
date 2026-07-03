namespace App1.Backend.Models;

public sealed class BffSessionData
{
    public required string SessionId { get; init; }
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public string? Subject { get; set; }
    public string? PreferredUsername { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public sealed class OidcPendingLogin
{
    public required string State { get; init; }
    public required string CodeVerifier { get; init; }
    public string? Prompt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string? refresh_token { get; set; }
    public string? id_token { get; set; }
    public int expires_in { get; set; }
}

public sealed class UserInfoDto
{
    public string Subject { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public sealed class SessionStatusResponse
{
    public bool Authenticated { get; set; }
    public UserInfoDto? User { get; set; }
}

public sealed class ApiErrorResponse
{
    public required string Message { get; set; }
    public string? CorrelationId { get; set; }
}
