using System.ComponentModel.DataAnnotations;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ChangePasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string OldPassword { get; set; }
    
    [Password(true)]
    public required string NewPassword { get; set; }
}