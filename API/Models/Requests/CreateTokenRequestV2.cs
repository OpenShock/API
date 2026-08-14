namespace OpenShock.API.Models.Requests;

public sealed class CreateTokenRequestV2 : EditTokenRequestV2
{
    public DateTime? ValidUntil { get; set; } = null;
}
