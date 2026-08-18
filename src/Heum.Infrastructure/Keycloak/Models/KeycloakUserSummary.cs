using System.Text.Json.Serialization;

namespace Heum.Infrastructure.Keycloak.Models;

public sealed class KeycloakUserSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("createdTimestamp")]
    public long? CreatedTimestamp { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string[]>? Attributes { get; set; }
}
