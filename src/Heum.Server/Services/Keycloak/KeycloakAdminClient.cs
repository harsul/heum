using Microsoft.Extensions.Options;

namespace Heum.Server.Services.Keycloak;

/// <summary>
/// Talks to the Keycloak Admin REST API to provision users as part of tenant onboarding.
/// Authenticates using the client-credentials grant for the "tenant-provisioning-service"
/// confidential client, which has been granted the realm-management "manage-users" role.
/// </summary>
public class KeycloakAdminClient(HttpClient httpClient, IOptions<KeycloakAdminOptions> options)
    : IKeycloakAdminClient
{
    private readonly KeycloakAdminOptions _options = options.Value;

    public async Task<string> ProvisionTenantAdminUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        return await CreateUserAsync(accessToken, username, email, firstName, lastName, password, tenantId, cancellationToken);
    }

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_options.Realm}/protocol/openid-connect/token");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Keycloak did not return an access token.");

        return token.AccessToken;
    }

    private async Task<string> CreateUserAsync(
        string accessToken,
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var user = new KeycloakUserRepresentation
        {
            Username = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Attributes = new Dictionary<string, string[]>
            {
                ["tenant_id"] = [tenantId.ToString()],
            },
            Credentials =
            [
                new KeycloakCredentialRepresentation { Type = "password", Value = password, Temporary = false },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(user),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Keycloak returns the new user's location in the Location header: .../users/{id}
        var location = response.Headers.Location
            ?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user.");

        return location.Segments[^1].TrimEnd('/');
    }
}
