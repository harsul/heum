using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Plans.Models;

public sealed class UpdatePlanRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
