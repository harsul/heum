using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heum.Infrastructure.Keycloak.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Heum.Infrastructure.Keycloak;

/// <summary>
/// Calls the Keycloak Admin REST API. Authenticates using the client-credentials grant for
/// the "tenant-provisioning-service" confidential client, which has been granted the
/// realm-management "manage-users" role. Contains no business logic - see
/// <see cref="IKeycloakService"/> for tenant-oriented operations built on top of this.
/// </summary>
internal sealed class KeycloakAdminClient(
    HttpClient httpClient,
    IOptions<KeycloakAdminOptions> options,
    IDistributedCache cache)
    : IKeycloakAdminClient
{
    private readonly KeycloakAdminOptions _options = options.Value;
    private const string AccessTokenCacheKey = "keycloak:admin:access_token";
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromSeconds(30);

    public async Task<string> CreateUserAsync(
        KeycloakUserRepresentation user,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users");
        request.Content = JsonContent.Create(user);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        var escapedQuery = Uri.EscapeDataString(query);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{_options.Realm}/users?q={escapedQuery}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/admin/realms/{_options.Realm}/users/{userId}/execute-actions-email");
        request.Content = JsonContent.Create(actions.ToArray());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var cachedToken = await cache.GetStringAsync(AccessTokenCacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedToken))
            return cachedToken;

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

        var cacheOptions = new DistributedCacheEntryOptions();
        if (token.ExpiresIn > 0)
        {
            var expiry = TimeSpan.FromSeconds(token.ExpiresIn) - TokenExpiryBuffer;
            if (expiry > TimeSpan.Zero)
                cacheOptions.AbsoluteExpirationRelativeToNow = expiry;
        }

        await cache.SetStringAsync(AccessTokenCacheKey, token.AccessToken, cacheOptions, cancellationToken);

        return token.AccessToken;
    }
}
