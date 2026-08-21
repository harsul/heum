using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heum.Infrastructure.Keycloak.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Heum.Infrastructure.Keycloak.Clients;

/// <summary>
/// Attaches an admin Bearer token to every outgoing Keycloak Admin API request.
/// The token is fetched via client_credentials and cached in distributed cache to
/// avoid a round-trip on each call. Token requests go through a separate, dedicated
/// <see cref="HttpClient"/> (see <see cref="TokenClientName"/>) rather than this handler's
/// own pipeline, so they resolve service discovery/BaseAddress correctly and never carry
/// a (nonexistent) Bearer header.
/// </summary>
internal sealed class KeycloakAdminAuthHandler(
    IOptions<KeycloakAdminOptions> options,
    IDistributedCache cache,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    public const string TokenClientName = "KeycloakAdminToken";

    private readonly KeycloakAdminOptions _options = options.Value;
    private const string AccessTokenCacheKey = "keycloak:admin:access_token";
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(AccessTokenCacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return cached;

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/realms/{_options.Realm}/protocol/openid-connect/token");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        });

        // Use a dedicated HttpClient (not base.SendAsync) so the request goes through
        // HttpClient.SendAsync itself - which is what resolves BaseAddress/service discovery
        // for the "http+https://" scheme - and so the token request never carries an
        // Authorization header (this client has no auth handler attached).
        using var tokenClient = httpClientFactory.CreateClient(TokenClientName);
        using var response = await tokenClient.SendAsync(tokenRequest, cancellationToken);
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
