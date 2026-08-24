using System.Text.Json.Serialization;

namespace Heum.Infrastructure.Keycloak.Models;

internal sealed class KeycloakRoleRepresentation
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    // Keycloak stores attribute values as List<string> even for single values.
    // e.g. { "roleType": ["Application"] }
    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; init; }
}
