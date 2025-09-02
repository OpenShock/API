using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Options;

public sealed class DiscordOAuthOptions
{
    public const string SectionName = "OpenShock:Discord";

    [Required(AllowEmptyStrings = false)]
    public required string ClientId { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ClientSecret { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string RedirectUri { get; init; }
}

[OptionsValidator]
public partial class DiscordOAuthOptionsValidator : IValidateOptions<DiscordOAuthOptions>
{
}
