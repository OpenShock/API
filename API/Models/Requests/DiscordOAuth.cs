using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class DiscordOAuth
{
    [Required]
    public required string Code { get; init; }
}
