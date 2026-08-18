using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Admin.Tenants.Models;

public class AddTenantUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
