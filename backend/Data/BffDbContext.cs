using App1.Backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App1.Backend.Data;

public sealed class BffDbContext : DbContext
{
    public BffDbContext(DbContextOptions<BffDbContext> options) : base(options)
    {
    }

    public DbSet<BffSessionEntity> Sessions => Set<BffSessionEntity>();
    public DbSet<BffPendingLoginEntity> PendingLogins => Set<BffPendingLoginEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BffSessionEntity>(entity =>
        {
            entity.ToTable("bff_sessions");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.EncryptedAccessToken).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(256);
            entity.Property(e => e.PreferredUsername).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(512);
            entity.Property(e => e.Name).HasMaxLength(512);
            entity.HasIndex(e => e.LastSeenAt);
        });

        modelBuilder.Entity<BffPendingLoginEntity>(entity =>
        {
            entity.ToTable("bff_pending_logins");
            entity.HasKey(e => e.PendingId);
            entity.Property(e => e.PendingId).HasMaxLength(64);
            entity.Property(e => e.State).IsRequired().HasMaxLength(128);
            entity.Property(e => e.CodeVerifier).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Prompt).HasMaxLength(32);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
