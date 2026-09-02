using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Subscriptions.Models;

public sealed class AssignPlanRequest
{
    [Required]
    public Guid PlanId { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
