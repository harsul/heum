namespace Heum.Server.Features.Tenants.Models;

public class TenantUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public bool EmailVerified { get; set; }
    public DateTimeOffset? CreatedAtUtc { get; set; }
}
