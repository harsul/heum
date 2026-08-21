using System.Linq.Expressions;
using Heum.Data.Auditing;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Data.Multitenancy;
using Heum.Data.SoftDelete;
using Microsoft.EntityFrameworkCore;

namespace Heum.Data;

public class HeumDbContext(DbContextOptions<HeumDbContext> options, ITenantProvider? tenantProvider = null) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isTenantScoped = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType);

            if (!isTenantScoped && !isSoftDeletable)
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            Expression? filter = null;

            if (isTenantScoped)
            {
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var currentTenantId = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
                filter = Expression.Equal(tenantIdProperty, currentTenantId);
            }

            if (isSoftDeletable)
            {
                var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Not(isDeletedProperty);
                filter = filter is null ? notDeleted : Expression.AndAlso(filter, notDeleted);
            }

            entityType.SetQueryFilter(Expression.Lambda(filter!, parameter));
        }
    }

    public Guid CurrentTenantId => tenantProvider?.TenantId ?? Guid.Empty;
}
