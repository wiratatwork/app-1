namespace App1.Backend.Data.Entities;

public sealed class BffPendingLoginEntity
{
    public string PendingId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
