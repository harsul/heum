using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasMany(p => p.Entitlements)
            .WithOne(pe => pe.Plan)
            .HasForeignKey(pe => pe.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(new
        {
            Id = WellKnownIds.FreePlanId,
            Name = "Free",
            IsActive = true,
            CreatedAtUtc = SeedDate,
            UpdatedAtUtc = (DateTime?)null,
        });
    }
}
