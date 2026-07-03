namespace App1.Backend.Configuration;

public sealed class KeycloakSettings
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = "http://localhost:7890";
    /// <summary>Browser-facing Keycloak URL (e.g. http://localhost:7890). Falls back to BaseUrl when empty.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = "toyota";
    public string ClientId { get; set; } = "app-1";
    public string Scope { get; set; } = "openid profile email";
    public string Audience { get; set; } = "account";

    private string PublicOrigin => (string.IsNullOrWhiteSpace(PublicBaseUrl) ? BaseUrl : PublicBaseUrl).TrimEnd('/');

    public string InternalAuthority => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}";

    public string PublicAuthority => $"{PublicOrigin}/realms/{Realm}";

    public string AuthorizationEndpoint => $"{PublicAuthority}/protocol/openid-connect/auth";

    public string TokenEndpoint => $"{InternalAuthority}/protocol/openid-connect/token";

    public string LogoutEndpoint => $"{PublicAuthority}/protocol/openid-connect/logout";

    public string MetadataAddress => $"{InternalAuthority}/.well-known/openid-configuration";
}

public sealed class BffSettings
{
    public const string SectionName = "Bff";

    public string PublicBaseUrl { get; set; } = "http://localhost:4201";
    public string SessionCookieName { get; set; } = "app1_bff_session";
    public string PendingLoginCookieName { get; set; } = "app1_bff_pending";
    public int SessionIdleMinutes { get; set; } = 30;
    public bool EnableSilentSso { get; set; } = true;
    public string PostLoginPath { get; set; } = "/";
    public string PostLogoutPath { get; set; } = "/login";
}

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:4201"];
}

public sealed class TokenEncryptionSettings
{
    public const string SectionName = "Bff:TokenEncryption";

    /// <summary>Base64-encoded 32-byte AES-256 key.</summary>
    public string Key { get; set; } = string.Empty;
}
