using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Data.Contexts;

public class HeumDbContext(DbContextOptions<HeumDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumDbContext).Assembly);
    }
}