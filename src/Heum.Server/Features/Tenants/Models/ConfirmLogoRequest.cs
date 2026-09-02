using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Features.Tenants.Models;

public record ConfirmLogoRequest([Required] string LogoUrl);
