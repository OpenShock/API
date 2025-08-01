namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddEmailProviderBlacklistDto
{
    public required string[] Domains { get; init; }
}
