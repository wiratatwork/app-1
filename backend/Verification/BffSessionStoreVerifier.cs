using App1.Backend.Configuration;
using App1.Backend.Data;
using App1.Backend.Models;
using App1.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App1.Backend.Verification;

public static class BffSessionStoreVerifier
{
    public static async Task RunAsync(string connectionString, string encryptionKeyBase64)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.Configure<BffSettings>(options => options.SessionIdleMinutes = 30);
        services.Configure<TokenEncryptionSettings>(options => options.Key = encryptionKeyBase64);
        services.AddDbContextFactory<BffDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<TokenCipherService>();
        services.AddSingleton<BffSessionStore>();

        await using var provider = services.BuildServiceProvider();
        var dbFactory = provider.GetRequiredService<IDbContextFactory<BffDbContext>>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.Database.MigrateAsync();
        }

        var store = provider.GetRequiredService<BffSessionStore>();
        var tokens = new TokenResponse
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            id_token = "test-id-token",
            expires_in = 300,
        };
        var user = new UserInfoDto
        {
            Subject = "test-subject",
            Username = "demo",
            Email = "demo@example.com",
            Name = "Demo User",
        };

        var created = store.CreateSession(tokens, user);
        var loaded = store.GetSession(created.SessionId);
        if (loaded is null || loaded.AccessToken != tokens.access_token)
        {
            throw new InvalidOperationException("Session could not be loaded immediately after creation.");
        }

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Sessions.AsNoTracking().SingleAsync(s => s.SessionId == created.SessionId);
            if (row.EncryptedAccessToken == tokens.access_token)
            {
                throw new InvalidOperationException("Access token was stored in plaintext.");
            }
        }

        using var secondProvider = services.BuildServiceProvider();
        var reloadedStore = secondProvider.GetRequiredService<BffSessionStore>();
        var reloaded = reloadedStore.GetSession(created.SessionId);
        if (reloaded is null || reloaded.RefreshToken != tokens.refresh_token)
        {
            throw new InvalidOperationException("Session did not survive a new store instance (simulated API restart).");
        }

        store.RemoveSession(created.SessionId);
        if (store.GetSession(created.SessionId) is not null)
        {
            throw new InvalidOperationException("Session was not removed.");
        }

        Console.WriteLine("BffSessionStore verification passed.");
    }
}
