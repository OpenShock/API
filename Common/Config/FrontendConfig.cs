using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Config;

public sealed class FrontendConfig
{
    [Required] public required Uri BaseUrl { get; init; }
    [Required] public required Uri ShortUrl { get; init; }
    [Required(AllowEmptyStrings = false)] public required string CookieDomain { get; init; }
}