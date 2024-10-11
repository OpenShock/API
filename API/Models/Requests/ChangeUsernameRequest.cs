namespace OpenShock.API.Models.Requests;

public sealed class ChangeUsernameRequest
{
    public required string Username { get; init; }
}