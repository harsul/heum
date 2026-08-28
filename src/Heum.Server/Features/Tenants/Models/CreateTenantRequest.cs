using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants.Models;

public class CreateTenantRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;
}
