namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddWebhookDto
{
    public required string Name { get; set; }
    public required Uri Url { get; set; }
}