namespace OpenShock.API.Models.Requests;

public sealed class ChangePasswordRequest
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}