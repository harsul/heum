using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Subscriptions.Models;

public sealed class EntitlementOverrideRequest
{
    [Required]
    public string Value { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; init; }
}
