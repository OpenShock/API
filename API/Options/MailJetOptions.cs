using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Options;

public sealed class MailJetOptions
{
    public const string SectionName = MailOptions.SectionName + ":Mailjet";

    [Required(AllowEmptyStrings = false)]
    public required string Key { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Secret { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required MailjetTemplateOptions Template { get; init; }

    public sealed class MailjetTemplateOptions
    {
        [Required]
        public ulong ActivateAccount { get; set; }
        
        [Required]
        public required ulong PasswordReset { get; init; }

        [Required]
        public required ulong PasswordResetComplete { get; init; }

        [Required]
        public required ulong VerifyEmail { get; init; }

        [Required]
        public required ulong VerifyEmailComplete { get; init; }
    }
}

[OptionsValidator]
public partial class MailJetOptionsValidator : IValidateOptions<MailJetOptions>
{
}