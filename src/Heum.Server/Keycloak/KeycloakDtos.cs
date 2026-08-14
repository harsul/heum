using System.Text.Json.Serialization;

namespace Heum.Server.Keycloak;

internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}

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
}

internal sealed class KeycloakCredentialRepresentation
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "password";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("temporary")]
    public bool Temporary { get; set; }
}

