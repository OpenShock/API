using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using OpenShock.Common.Options;

namespace OpenShock.Cron.Options;

public sealed class SmtpOptions
{
    public const string SectionName = MailOptions.SectionName + ":Smtp";

    [Required(AllowEmptyStrings = false)]
    public required string Host { get; init; }

    public ushort Port { get; init; } = 587;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool EnableSsl { get; init; } = true;

    public bool VerifyCertificate { get; init; } = true;
}

[OptionsValidator]
public sealed partial class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
}
