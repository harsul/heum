using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.Property(s => s.ChangedByUserId)
            .HasMaxLength(200);

        builder.Property(s => s.EffectiveAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        // Fast lookup: "what is tenant X's current plan?"
        builder.HasIndex(s => new { s.TenantId, s.EffectiveAtUtc });

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
