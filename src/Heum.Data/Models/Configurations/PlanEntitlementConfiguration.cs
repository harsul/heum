using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.HasKey(pe => new { pe.PlanId, pe.EntitlementId });

        builder.Property(pe => pe.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(pe => pe.Entitlement)
            .WithMany()
            .HasForeignKey(pe => pe.EntitlementId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Free plan entitlement values
        builder.HasData(
            new { PlanId = WellKnownIds.FreePlanId, EntitlementId = WellKnownIds.MaxUsersEntitlementId,               Value = "5" },
            new { PlanId = WellKnownIds.FreePlanId, EntitlementId = WellKnownIds.MaxInvitationsPerMonthEntitlementId, Value = "20" },
            new { PlanId = WellKnownIds.FreePlanId, EntitlementId = WellKnownIds.CanUploadLogoEntitlementId,          Value = "false" }
        );
    }
}
