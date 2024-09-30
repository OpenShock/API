using OpenShock.ServicesCommon.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class SignUpV2
{
    [Username(true)]
    public required string Username { get; set; }
    [StringLength(256, MinimumLength = 12)]
    public required string Password { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
    [Required(AllowEmptyStrings = false)] public required string TurnstileResponse { get; set; }
}