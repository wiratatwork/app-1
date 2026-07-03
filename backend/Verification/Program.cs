using App1.Backend.Verification;

var connectionString = args.ElementAtOrDefault(0)
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Port=5434;Database=app1;Username=app1;Password=changeme";
var encryptionKey = args.ElementAtOrDefault(1)
    ?? Environment.GetEnvironmentVariable("Bff__TokenEncryption__Key")
    ?? "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

await BffSessionStoreVerifier.RunAsync(connectionString, encryptionKey);
