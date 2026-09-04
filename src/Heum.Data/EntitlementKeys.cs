namespace Heum.Data;

/// <summary>
/// Keys of the seeded entitlements (see <c>EntitlementConfiguration</c>). Referenced from
/// enforcement points so a typo can't silently disable a check.
/// </summary>
public static class EntitlementKeys
{
    public const string MaxUsers = "max_users";
    public const string MaxInvitationsPerMonth = "max_invitations_per_month";
    public const string CanUploadLogo = "can_upload_logo";
}
