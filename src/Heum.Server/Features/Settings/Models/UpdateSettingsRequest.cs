using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Settings.Models;

public sealed class UpdateSettingsRequest
{
    [Required]
    [MaxLength(10)]
    public required string Locale { get; init; }

    [Required]
    [MaxLength(100)]
    public required string Timezone { get; init; }
}
