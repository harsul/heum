namespace Heum.Server.Tenants;

public class RegisterTenantResponse
{
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string KeycloakUserId { get; set; } = string.Empty;
}
