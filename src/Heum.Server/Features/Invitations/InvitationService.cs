using System.Net;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Invitations;

internal sealed class InvitationService(
    HeumDbContext dbContext,
    IKeycloakService keycloakService,
    IDomainEventCollector domainEventCollector,
    TimeProvider timeProvider) : IInvitationService
{
    private static readonly TimeSpan InvitationValidity = TimeSpan.FromDays(7);

    public async Task<InvitationResult> CreateAsync(
        Guid tenantId,
        string email,
        string invitedByUserId,
        CancellationToken cancellationToken = default)
    {
        var hasPending = await dbContext.Invitations.AnyAsync(
            i => i.TenantId == tenantId
                 && i.Email == email
                 && i.Status == InvitationStatus.Pending
                 && i.ExpiresAtUtc > timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (hasPending)
            return new InvitationResult(null, DuplicatePending: true);

        var invitation = Invitation.Create(tenantId, email, invitedByUserId, InvitationValidity, timeProvider);
        dbContext.Invitations.Add(invitation);

        domainEventCollector.Enqueue(new InvitationCreatedEvent(
            invitation.Id,
            tenantId,
            email,
            invitation.Token,
            timeProvider.GetUtcNow()));

        await dbContext.SaveChangesAsync(cancellationToken);
        return new InvitationResult(invitation, DuplicatePending: false);
    }

    public async Task<IReadOnlyList<Invitation>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Invitations
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<AcceptResult> AcceptAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.Invitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invitation is null || !invitation.Accept(timeProvider))
            return new AcceptResult(Accepted: false, EmailConflict: false);

        try
        {
            var keycloakUserId = await keycloakService.CreateTenantUserAsync(
                invitation.Email, invitation.TenantId, isTenantAdmin: false, cancellationToken);

            domainEventCollector.Enqueue(new UserOnboardingRequestedEvent(
                invitation.TenantId,
                invitation.Email,
                keycloakUserId,
                timeProvider.GetUtcNow()));

            await dbContext.SaveChangesAsync(cancellationToken);
            return new AcceptResult(Accepted: true, EmailConflict: false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return new AcceptResult(Accepted: false, EmailConflict: true);
        }
    }

    public async Task<bool> RevokeAsync(
        Guid tenantId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.TenantId == tenantId, cancellationToken);

        if (invitation is null || !invitation.Revoke(timeProvider))
            return false;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
