using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using App1.Backend.Configuration;
using App1.Backend.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace App1.Backend.Services;

public sealed class KeycloakOidcService
{
    private readonly KeycloakSettings _keycloak;
    private readonly BffSettings _bff;
    private readonly HttpClient _httpClient;

    public KeycloakOidcService(
        IOptions<KeycloakSettings> keycloak,
        IOptions<BffSettings> bff,
        HttpClient httpClient)
    {
        _keycloak = keycloak.Value;
        _bff = bff.Value;
        _httpClient = httpClient;
    }

    public string CallbackUrl => $"{_bff.PublicBaseUrl.TrimEnd('/')}/api/bff/auth/callback";

    public (string AuthUrl, OidcPendingLogin Pending) BuildAuthorizationRequest(string? prompt = null)
    {
        var state = RandomString(24);
        var codeVerifier = RandomString(48);
        var codeChallenge = Sha256Base64Url(codeVerifier);

        var pending = new OidcPendingLogin
        {
            State = state,
            CodeVerifier = codeVerifier,
            Prompt = prompt,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _keycloak.ClientId,
            ["redirect_uri"] = CallbackUrl,
            ["response_type"] = "code",
            ["scope"] = _keycloak.Scope,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
        };
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            query["prompt"] = prompt;
        }

        var authUrl = QueryHelpers.AddQueryString(_keycloak.AuthorizationEndpoint, query);
        return (authUrl, pending);
    }

    public async Task<TokenResponse?> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _keycloak.ClientId,
            ["code"] = code,
            ["redirect_uri"] = CallbackUrl,
            ["code_verifier"] = codeVerifier,
        };

        return await RequestTokenAsync(body, cancellationToken);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _keycloak.ClientId,
            ["refresh_token"] = refreshToken,
        };

        return await RequestTokenAsync(body, cancellationToken);
    }

    public string BuildLogoutUrl(string? idTokenHint)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _keycloak.ClientId,
            ["post_logout_redirect_uri"] = $"{_bff.PublicBaseUrl.TrimEnd('/')}{_bff.PostLogoutPath}",
        };
        if (!string.IsNullOrWhiteSpace(idTokenHint))
        {
            query["id_token_hint"] = idTokenHint;
        }

        return QueryHelpers.AddQueryString(_keycloak.LogoutEndpoint, query);
    }

    private async Task<TokenResponse?> RequestTokenAsync(Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _keycloak.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(body),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<TokenResponse>(stream, cancellationToken: cancellationToken);
    }

    private static string RandomString(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Sha256Base64Url(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
