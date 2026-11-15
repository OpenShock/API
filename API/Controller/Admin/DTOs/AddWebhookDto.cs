using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddWebhookDto
{
    [StringLength(ApiHardLimits.DiscordWebhookNameMaxLength)]
    public required string Name { get; init; }
    public required Uri Url { get; init; }
}
