using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Options;

public sealed class FrontendOptions
{
    public const string SectionName = "OpenShock:Frontend";

    [Required]
    public required Uri BaseUrl { get; init; }

    [Required]
    public required Uri ShortUrl { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string CookieDomain { get; init; }
}

[OptionsValidator]
public partial class FrontendOptionsValidator : IValidateOptions<FrontendOptions>
{
}