using Heum.Server.Features.Tenants.Services;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Records invalidations so tests can assert the status cache is dropped on (de)activation.</summary>
public sealed class FakeTenantStatusService : ITenantStatusService
{
    public List<Guid> Invalidated { get; } = [];

    public ValueTask<bool> IsActiveAsync(Guid tenantId, CancellationToken cancellationToken = default) => new(true);

    public Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Invalidated.Add(tenantId);
        return Task.CompletedTask;
    }
}
