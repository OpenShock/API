namespace OpenShock.API.Services.Account;

public readonly struct LoginContext
{
    public required string UserAgent { get; init; }
    public required string Ip { get; init; }
}