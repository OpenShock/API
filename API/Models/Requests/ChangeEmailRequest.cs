using System.ComponentModel.DataAnnotations;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ChangeEmailRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string CurrentPassword { get; set; }

    [OpenShock.Common.DataAnnotations.EmailAddress(true)]
    public required string Email { get; set; }
}