using Heum.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Data;

public class HeumdDbContext(DbContextOptions<HeumdDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumdDbContext).Assembly);
    }
}
