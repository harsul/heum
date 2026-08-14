using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants;

public class RegisterTenantRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric words separated by hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string AdminLastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string AdminPassword { get; set; } = string.Empty;
}
