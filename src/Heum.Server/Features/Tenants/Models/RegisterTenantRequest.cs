using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants.Models;

public class RegisterTenantRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;
}
