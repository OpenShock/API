namespace OpenShock.API.Services.Account;

public struct LoginContext
{
    public required string UserAgent { get; init; }
    public required string Ip { get; init; }
}