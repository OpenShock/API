namespace OpenShock.API.Models.Response;

public sealed class LoginSessionResponse
{
    public required Guid Id { get; set; }
    public required string Ip { get; set; }
    public required string UserAgent { get; set; }
    public required DateTime Created { get; set; }
    public required DateTime Expires { get; set; }
}