using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class OAuthLinkPasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; set; }
}
