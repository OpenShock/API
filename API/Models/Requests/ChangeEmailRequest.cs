using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ChangeEmailRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string CurrentPassword { get; set; }

    [Common.DataAnnotations.EmailAddress(true)]
    public required string Email { get; set; }
}