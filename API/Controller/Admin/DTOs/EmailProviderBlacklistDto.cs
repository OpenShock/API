namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class EmailProviderBlacklistDto
{
    public required Guid Id { get; init; }

    public required string Domain { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
