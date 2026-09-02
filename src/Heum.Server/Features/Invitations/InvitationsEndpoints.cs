using System.Security.Claims;
using Heum.Data.Models;
using Heum.Server.Common;
using Heum.Server.Features.Invitations.Models;
using Heum.Server.Features.Invitations.Services;
using Heum.Server.Features.Tenants;
using Heum.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Invitations;

public static class InvitationsEndpoints
{
    public static RouteGroupBuilder MapInvitationsEndpoints(this RouteGroupBuilder group)
    {
        var invitations = group.MapGroup("/invitations");

        invitations.MapPost("/accept", AcceptInvitationAsync)
            .WithName("AcceptInvitation")
            .AllowAnonymous()
            .RequireRateLimiting("registration");

        var managed = invitations.RequireAuthorization("TenantAdmin");

        managed.MapGet("/", ListInvitationsAsync)
            .WithName("ListInvitations");

        managed.MapPost("/", CreateInvitationAsync)
            .WithName("CreateInvitation");

        managed.MapPost("/{id:guid}/revoke", RevokeInvitationAsync)
            .WithName("RevokeInvitation");

        return group;
    }

    internal static async Task<Results<Created<InvitationResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>, ForbidHttpResult>> CreateInvitationAsync(
        ITenantContext tenantContext,
        CreateInvitationRequest request,
        IInvitationService invitationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var invitedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await invitationService.CreateAsync(tenantContext.TenantId, request.Email, invitedBy, cancellationToken);

        if (result.EntitlementExceeded)
            return TypedResults.Forbid();

        if (result.DuplicatePending)
            return TypedResults.Conflict(InvitationProblems.DuplicatePending(request.Email));

        return TypedResults.Created(
            $"/api/invitations/{result.Invitation!.Id}",
            ToResponse(result.Invitation));
    }

    internal static async Task<Results<Ok<PagedResponse<InvitationResponse>>, BadRequest<ProblemDetails>>> ListInvitationsAsync(
        ITenantContext tenantContext,
        IInvitationService invitationService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = 25)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var (items, totalCount) = await invitationService.ListAsync(tenantContext.TenantId, search, page, pageSize, cancellationToken);
        return TypedResults.Ok(new PagedResponse<InvitationResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    internal static async Task<Results<Ok, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        IInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        var result = await invitationService.AcceptAsync(request.Token, cancellationToken);

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantProblems.EmailConflict(request.Token));

        if (!result.Accepted)
            return TypedResults.BadRequest(InvitationProblems.InvalidToken());

        return TypedResults.Ok();
    }

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> RevokeInvitationAsync(
        Guid id,
        ITenantContext tenantContext,
        IInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var revoked = await invitationService.RevokeAsync(tenantContext.TenantId, id, cancellationToken);
        return revoked ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static InvitationResponse ToResponse(Invitation invitation) => new()
    {
        Id = invitation.Id,
        Email = invitation.Email,
        Status = invitation.Status.ToString(),
        CreatedAtUtc = invitation.CreatedAtUtc,
        ExpiresAtUtc = invitation.ExpiresAtUtc,
        AcceptedAtUtc = invitation.AcceptedAtUtc,
        RevokedAtUtc = invitation.RevokedAtUtc,
    };
}
