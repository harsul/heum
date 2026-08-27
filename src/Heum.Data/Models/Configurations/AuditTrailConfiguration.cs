using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heum.Data.Models.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.ToTable("AuditTrails");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.PrimaryKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.TimestampUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(a => new { a.EntityName, a.PrimaryKey });
        builder.HasIndex(a => a.TimestampUtc);
    }
}
