using System.Text.Json.Serialization;

namespace Heum.Infrastructure.Keycloak.Models;

internal sealed class KeycloakUserRepresentation
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; } = true;

    [JsonPropertyName("attributes")]
    public Dictionary<string, string[]> Attributes { get; set; } = [];

    [JsonPropertyName("credentials")]
    public List<KeycloakCredentialRepresentation> Credentials { get; set; } = [];

    [JsonPropertyName("requiredActions")]
    public List<string> RequiredActions { get; set; } = [];

    [JsonPropertyName("realmRoles")]
    public List<string> RealmRoles { get; set; } = [];
}