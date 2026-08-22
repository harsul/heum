using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Invitations.Models;

public sealed class CreateInvitationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public required string Email { get; init; }
}
