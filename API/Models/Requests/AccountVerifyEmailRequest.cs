namespace OpenShock.API.Models.Requests;

public sealed class AccountVerifyEmailRequest
{
    public required string Secret { get; set; }
}