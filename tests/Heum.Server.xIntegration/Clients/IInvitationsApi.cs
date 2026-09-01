using Heum.Server.Common;
using Heum.Server.Features.Invitations.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface IInvitationsApi
{
    [Post("/api/invitations")]
    Task<IApiResponse<InvitationResponse>> CreateInvitationAsync(
        CreateInvitationRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/invitations")]
    Task<IApiResponse<PagedResponse<InvitationResponse>>> ListInvitationsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    [Post("/api/invitations/{id}/revoke")]
    Task<IApiResponse> RevokeInvitationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Post("/api/invitations/accept")]
    Task<IApiResponse> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default);
}
