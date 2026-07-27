using Microsoft.Extensions.Options;
using OpenShock.Common.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.Cron.Options;

public sealed class MailJetOptions
{
    public const string SectionName = MailOptions.SectionName + ":Mailjet";

    [Required(AllowEmptyStrings = false)]
    public required string Key { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Secret { get; init; }
}

[OptionsValidator]
public partial class MailJetOptionsValidator : IValidateOptions<MailJetOptions>
{
}