using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(100);

        // Ensure no two companies can register the same slug (e.g., myapp.com/acme)
        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");
    }
}
