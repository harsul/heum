using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.TenantId)
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.Timezone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");
    }
}
