namespace Heum.Server.Keycloak;

public class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AdminRoleName { get; set; } = "Admin";
}
