using App1.Backend.Authentication;
using App1.Backend.Configuration;
using App1.Backend.Data;
using App1.Backend.Middleware;
using App1.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KeycloakSettings>(builder.Configuration.GetSection(KeycloakSettings.SectionName));
builder.Services.Configure<BffSettings>(builder.Configuration.GetSection(BffSettings.SectionName));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
builder.Services.Configure<TokenEncryptionSettings>(builder.Configuration.GetSection(TokenEncryptionSettings.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContextFactory<BffDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<KeycloakOidcService>();
builder.Services.AddSingleton<TokenCipherService>();
builder.Services.AddSingleton<BffSessionStore>();
builder.Services.AddSingleton<BffCookieService>();
builder.Services.AddSingleton<BffTokenValidator>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BffDbContext>();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var keycloak = builder.Configuration.GetSection(KeycloakSettings.SectionName).Get<KeycloakSettings>() ?? new KeycloakSettings();
var corsOrigins = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins
    ?? ["http://localhost:4201"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bff";
    options.DefaultChallengeScheme = "Bff";
})
.AddScheme<BffAuthenticationOptions, BffAuthenticationHandler>("Bff", null)
.AddJwtBearer("Bearer", options =>
{
    options.Authority = keycloak.PublicAuthority;
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", false);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = keycloak.PublicAuthority,
        ValidateAudience = true,
        ValidAudiences = builder.Configuration.GetSection("Keycloak:ValidAudiences").Get<string[]>()
            ?? [keycloak.Audience, keycloak.ClientId, "account"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder("Bff")
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BffDbContext>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<BffDbContext>>();

    var retries = 10;
    while (retries > 0)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            logger.LogInformation("Applying BFF session migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("BFF session migrations applied.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "Database not ready ({Retries} retries left).", retries);
            if (retries == 0)
            {
                throw;
            }

            await Task.Delay(3000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCorrelationId();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
