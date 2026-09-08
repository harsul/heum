namespace Heum.Server.Features.Dashboard.Models;

public sealed record AdminStatsResponse(
    int TotalTenants,
    int ActiveTenants,
    int TotalPlans,
    int TotalEntitlements,
    IReadOnlyList<RecentTenantEntry> RecentTenants);

public sealed record RecentTenantEntry(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAtUtc);
