namespace Heum.Data;

/// <summary>
/// Deterministic GUIDs for seeded reference data so migrations are idempotent.
/// </summary>
public static class WellKnownIds
{
    // Plans
    public static readonly Guid FreePlanId  = new("00000000-0000-0000-0000-000000000001");

    // Entitlements
    public static readonly Guid MaxUsersEntitlementId              = new("00000000-0000-0000-0000-000000000010");
    public static readonly Guid MaxInvitationsPerMonthEntitlementId = new("00000000-0000-0000-0000-000000000011");
    public static readonly Guid CanUploadLogoEntitlementId         = new("00000000-0000-0000-0000-000000000012");
}
