using System.ComponentModel.DataAnnotations;
using Heum.Data.Models;

namespace Heum.Server.Features.Plans.Models;

public sealed class CreateEntitlementRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Key { get; init; } = string.Empty;

    [Required]
    public EntitlementType Type { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}
