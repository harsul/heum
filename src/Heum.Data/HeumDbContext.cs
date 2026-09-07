using System.Linq.Expressions;
using Heum.Data.Auditing;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Data.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Heum.Data;

public class HeumDbContext(DbContextOptions<HeumDbContext> options, ITenantProvider? tenantProvider = null) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
    public DbSet<PlanEntitlement> PlanEntitlements => Set<PlanEntitlement>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantEntitlementOverride> TenantEntitlementOverrides => Set<TenantEntitlementOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var currentTenantId = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
            var filter = Expression.Equal(tenantIdProperty, currentTenantId);

            entityType.SetQueryFilter(Expression.Lambda(filter, parameter));
        }
    }

    public Guid CurrentTenantId => tenantProvider?.TenantId ?? Guid.Empty;
}
