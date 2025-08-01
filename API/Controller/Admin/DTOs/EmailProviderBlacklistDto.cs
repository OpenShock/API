namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class EmailProviderBlacklistDto
{
    public required Guid Id { get; set; }

    public required string Domain { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
