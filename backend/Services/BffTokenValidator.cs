using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App1.Backend.Configuration;
using App1.Backend.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace App1.Backend.Services;

public sealed class BffTokenValidator
{
    private readonly KeycloakSettings _keycloak;
    private readonly IConfiguration _configuration;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public BffTokenValidator(IOptions<KeycloakSettings> keycloak, IConfiguration configuration)
    {
        _keycloak = keycloak.Value;
        _configuration = configuration;
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _keycloak.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = _configuration.GetValue("Keycloak:RequireHttpsMetadata", false) });
    }

    public async Task<(ClaimsPrincipal? Principal, UserInfoDto? User)> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var oidcConfig = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _keycloak.PublicAuthority,
                ValidateAudience = true,
                ValidAudiences = GetValidAudiences(),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            var principal = _tokenHandler.ValidateToken(accessToken, validationParameters, out _);
            var user = ExtractUserInfo(principal);
            return (principal, user);
        }
        catch
        {
            return (null, null);
        }
    }

    private IEnumerable<string> GetValidAudiences()
    {
        var audiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _keycloak.Audience,
            _keycloak.ClientId,
            "account",
        };

        var configured = _configuration.GetSection("Keycloak:ValidAudiences").Get<string[]>();
        if (configured is { Length: > 0 })
        {
            foreach (var audience in configured)
            {
                audiences.Add(audience);
            }
        }

        return audiences;
    }

    private static UserInfoDto ExtractUserInfo(ClaimsPrincipal principal)
    {
        return new UserInfoDto
        {
            Subject = principal.FindFirstValue("sub") ?? string.Empty,
            Username = principal.FindFirstValue("preferred_username"),
            Email = principal.FindFirstValue("email"),
            Name = principal.FindFirstValue("name"),
        };
    }
}
