using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class TenantEntitlementOverrideConfiguration : IEntityTypeConfiguration<TenantEntitlementOverride>
{
    public void Configure(EntityTypeBuilder<TenantEntitlementOverride> builder)
    {
        builder.HasKey(o => new { o.TenantId, o.EntitlementId });

        builder.Property(o => o.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Reason)
            .HasMaxLength(500);

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Entitlement)
            .WithMany()
            .HasForeignKey(o => o.EntitlementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
