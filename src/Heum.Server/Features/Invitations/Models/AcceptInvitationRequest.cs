using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Invitations.Models;

public sealed class AcceptInvitationRequest
{
    [Required]
    public required string Token { get; init; }
}
