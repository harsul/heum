using Heum.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(200);

        // PostgreSQL-specific: jsonb column type requires PostgreSQL; use "nvarchar(max)" / "text" for other providers.
        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(o => o.OccurredAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.FailedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.NextAttemptAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.LastError)
            .HasMaxLength(2000);

        // Covers the poller's fetch query: pending (not processed, not dead-lettered) and eligible now.
        // OccurredAtUtc as the third column lets Postgres sort the eligible set without a separate sort step.
        builder.HasIndex(o => new { o.ProcessedAtUtc, o.FailedAtUtc, o.OccurredAtUtc });
    }
}
