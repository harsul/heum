namespace Heum.Server.Features.Plans.Models;

public sealed class EntitlementResponse
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}
