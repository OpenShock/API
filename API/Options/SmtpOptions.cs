using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Options;

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
public partial class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
}