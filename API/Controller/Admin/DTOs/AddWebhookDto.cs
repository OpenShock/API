namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddWebhookDto
{
    public required string Name { get; init; }
    public required Uri Url { get; init; }
}
