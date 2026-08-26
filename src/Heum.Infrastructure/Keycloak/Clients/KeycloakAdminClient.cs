using System.Net.Http.Json;
using Heum.Infrastructure.Keycloak.Models;
using Microsoft.Extensions.Options;

namespace Heum.Infrastructure.Keycloak.Clients;

/// <summary>
/// Calls the Keycloak Admin REST API. Authenticates using the client-credentials grant for
/// the "dotnet-admin-api" confidential client, which has been granted the
/// realm-management "manage-users" and "view-realm" roles. Contains no business logic - see
/// <see cref="IKeycloakService"/> for tenant-oriented operations built on top of this.
/// Authorization is handled transparently by <see cref="KeycloakAdminAuthHandler"/>.
/// </summary>
internal sealed class KeycloakAdminClient(
    HttpClient httpClient,
    IOptions<KeycloakAdminOptions> options)
    : IKeycloakAdminClient
{
    private readonly KeycloakAdminOptions _options = options.Value;

    public async Task<string> CreateUserAsync(
        KeycloakUserRepresentation user,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users");
        request.Content = JsonContent.Create(user);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Keycloak returns the new user's location in the Location header: .../users/{id}
        var location = response.Headers.Location
            ?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user.");

        return location.Segments[^1].TrimEnd('/');
    }

    public async Task<IReadOnlyList<KeycloakUserSummary>> SearchUsersAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var escapedQuery = Uri.EscapeDataString(query);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{_options.Realm}/users?q={escapedQuery}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserSummary>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    public async Task ExecuteUserActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.OnboardingRedirectUri))
            throw new InvalidOperationException(
                "KeycloakAdmin:OnboardingRedirectUri is not configured. " +
                "Set it to the frontend base URL so Keycloak can redirect users after completing registration.");

        var clientId = Uri.EscapeDataString(_options.OnboardingClientId);
        var redirectUri = Uri.EscapeDataString(_options.OnboardingRedirectUri);
        var url = $"/admin/realms/{_options.Realm}/users/{userId}/execute-actions-email" +
                  $"?client_id={clientId}&redirect_uri={redirectUri}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = JsonContent.Create(actions.ToArray());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<KeycloakUserSummary?> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{_options.Realm}/users/{userId}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<KeycloakUserSummary>(cancellationToken: cancellationToken);
    }

    public async Task SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/admin/realms/{_options.Realm}/users/{userId}");
        request.Content = JsonContent.Create(new { enabled });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<KeycloakRoleRepresentation>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{_options.Realm}/roles?briefRepresentation=false");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<List<KeycloakRoleRepresentation>>(
            cancellationToken: cancellationToken);
        return roles ?? [];
    }
}
