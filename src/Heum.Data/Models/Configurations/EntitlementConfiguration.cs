using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class EntitlementConfiguration : IEntityTypeConfiguration<Entitlement>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Entitlement> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.Key)
            .IsUnique();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasData(
            new { Id = WellKnownIds.MaxUsersEntitlementId,               Key = "max_users",                    Type = EntitlementType.Integer, IsActive = true, Description = (string?)"Maximum number of users allowed in the tenant" },
            new { Id = WellKnownIds.MaxInvitationsPerMonthEntitlementId, Key = "max_invitations_per_month",     Type = EntitlementType.Integer, IsActive = true, Description = (string?)"Maximum invitations that can be sent per month" },
            new { Id = WellKnownIds.CanUploadLogoEntitlementId,          Key = "can_upload_logo",              Type = EntitlementType.Boolean, IsActive = true, Description = (string?)"Whether the tenant can upload a custom logo" }
        );
    }
}
