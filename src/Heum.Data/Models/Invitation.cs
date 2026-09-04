using Heum.Data.Auditing;
using Heum.Data.Multitenancy;

namespace Heum.Data.Models;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked,
}

public class Invitation : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;

    /// <summary>One-time secret that lets the invitee accept; never written to the audit trail.</summary>
    [AuditRedacted]
    public string Token { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? InvitedByUserId { get; private set; }

    private Invitation() { }

    public static Invitation Create(
        Guid tenantId,
        string email,
        string invitedByUserId,
        TimeSpan validity,
        TimeProvider timeProvider) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Email = email,
        Token = Guid.NewGuid().ToString("N"),
        InvitedByUserId = invitedByUserId,
        CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        ExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.Add(validity),
    };

    public bool IsExpired(TimeProvider timeProvider) =>
        Status == InvitationStatus.Pending && timeProvider.GetUtcNow().UtcDateTime > ExpiresAtUtc;

    public bool Accept(TimeProvider timeProvider)
    {
        if (Status != InvitationStatus.Pending || IsExpired(timeProvider))
            return false;

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        return true;
    }

    public bool Revoke(TimeProvider timeProvider)
    {
        if (Status != InvitationStatus.Pending)
            return false;

        Status = InvitationStatus.Revoked;
        RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        return true;
    }
}
