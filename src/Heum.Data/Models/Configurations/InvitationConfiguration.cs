using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.HasIndex(i => new { i.TenantId, i.Email });

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.InvitedByUserId)
            .HasMaxLength(200);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.AcceptedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.RevokedAtUtc)
            .HasColumnType("timestamp with time zone");
    }
}
