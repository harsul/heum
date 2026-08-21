using Heum.Data.Auditing;
using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Data;

public class HeumDbContext(DbContextOptions<HeumDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumDbContext).Assembly);
    }
}
