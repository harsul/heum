using Heum.Server.Common;

namespace Heum.Server.Features.Tenants.Models;

// Kept as a named type so existing Refit clients and references compile without change.
public sealed class TenantHistoryResponse : PagedResponse<TenantHistoryEntryResponse>;
