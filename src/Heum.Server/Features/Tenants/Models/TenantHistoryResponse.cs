namespace Heum.Server.Features.Tenants.Models;

public class TenantHistoryResponse
{
    public IReadOnlyList<TenantHistoryEntryResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
