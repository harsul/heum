using Heum.Data.Models;

namespace Heum.Server.Features.Invitations.Services;

public sealed record InvitationResult(Invitation? Invitation, bool DuplicatePending);
public sealed record AcceptResult(bool Accepted, bool EmailConflict);

public interface IInvitationService
{
    Task<InvitationResult> CreateAsync(Guid tenantId, string email, string invitedByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<AcceptResult> AcceptAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid tenantId, Guid invitationId, CancellationToken cancellationToken = default);
}
