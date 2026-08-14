using System.Text.Json.Serialization;

namespace Heum.Server.Services.Keycloak.Models;

internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}