using Heum.Data;
using Heum.Server.Features.Dashboard.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Dashboard.Endpoints;

public static class AdminDashboardEndpoints
{
    public static RouteGroupBuilder MapAdminDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/stats", GetStatsAsync).WithName("AdminGetStats");
        return group;
    }

    internal static async Task<Ok<AdminStatsResponse>> GetStatsAsync(
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var totalTenants = await dbContext.Tenants.CountAsync(cancellationToken);
        var activeTenants = await dbContext.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var totalPlans = await dbContext.Plans.CountAsync(cancellationToken);
        var totalEntitlements = await dbContext.Entitlements.CountAsync(cancellationToken);

        var recentTenants = await dbContext.Tenants
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new RecentTenantEntry(t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new AdminStatsResponse(
            totalTenants,
            activeTenants,
            totalPlans,
            totalEntitlements,
            recentTenants));
    }
}
