namespace OpenShock.API.Models.Requests;

public class InvalidateTokensRequest
{
    public required IEnumerable<string> Secrets { get; set; }
}