namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddEmailProviderBlacklistDto
{
    public required string Domain { get; init; }
}
