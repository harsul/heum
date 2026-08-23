using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants.Models;

public class UpdateTenantRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
