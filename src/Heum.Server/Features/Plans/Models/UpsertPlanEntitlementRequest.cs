using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Plans.Models;

public sealed class UpsertPlanEntitlementRequest
{
    [Required]
    public string Value { get; init; } = string.Empty;
}
