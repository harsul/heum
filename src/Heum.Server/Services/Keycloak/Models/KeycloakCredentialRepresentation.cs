using System.Text.Json.Serialization;

namespace Heum.Server.Services.Keycloak.Models;

internal sealed class KeycloakCredentialRepresentation
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "password";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("temporary")]
    public bool Temporary { get; set; }
}