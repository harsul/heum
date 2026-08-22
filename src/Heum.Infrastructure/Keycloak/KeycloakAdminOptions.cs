using System.ComponentModel.DataAnnotations;

namespace Heum.Infrastructure.Keycloak;

public class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    // The Keycloak client ID to use when generating the onboarding action-email link.
    // Keycloak validates the redirect_uri against this client's allowed redirect URIs.
    public string OnboardingClientId { get; set; } = "react-frontend";

    // Where Keycloak should redirect the user after completing the required actions
    // (set password). Must be an allowed redirect URI for OnboardingClientId.
    // Only required for the Functions project; the server does not send action emails.
    public string OnboardingRedirectUri { get; set; } = string.Empty;
}
