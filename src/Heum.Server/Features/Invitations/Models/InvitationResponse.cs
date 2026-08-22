namespace Heum.Server.Features.Invitations.Models;

public sealed class InvitationResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
}
