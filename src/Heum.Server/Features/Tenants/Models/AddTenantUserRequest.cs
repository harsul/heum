using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants.Models;

public class AddTenantUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Role { get; set; }
}
