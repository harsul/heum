using System.Net;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Features.Plans.Services;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Invitations.Services;

internal sealed class InvitationService(
    HeumDbContext dbContext,
    IKeycloakService keycloakService,
    IDomainEventCollector domainEventCollector,
    IEntitlementService entitlementService,
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

        var maxUsers = await entitlementService.GetIntAsync(tenantId, "max_users", fallback: int.MaxValue, cancellationToken);
        var currentUsers = await keycloakService.ListTenantUsersAsync(tenantId, cancellationToken);
        if (currentUsers.Count >= maxUsers)
            return new InvitationResult(null, DuplicatePending: false, EntitlementExceeded: true);

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

    public async Task<(IReadOnlyList<Invitation> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Invitations
            .Where(i => i.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.Email.Contains(search));

        query = query.OrderByDescending(i => i.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AcceptResult> AcceptAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invitation is null || !invitation.Accept(timeProvider))
            return new AcceptResult(Accepted: false, EmailConflict: false);

        try
        {
            var keycloakUserId = await keycloakService.CreateTenantUserAsync(
                invitation.Email, invitation.TenantId, role: null, cancellationToken);

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
