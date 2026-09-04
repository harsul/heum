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

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(o => o.OccurredAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.NextAttemptAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.LastError)
            .HasMaxLength(2000);

        // Speeds up the poller's "give me the next unprocessed batch, oldest first" query.
        builder.HasIndex(o => new { o.ProcessedAtUtc, o.Attempts, o.OccurredAtUtc });
    }
}
