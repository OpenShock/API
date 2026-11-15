using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ControlStatChangesRequest
{
    /// <summary>The cursor returned by the previous call, omit for an initial sync of every requested user.</summary>
    public DateTime? Since { get; init; }

    /// <summary>The users the caller is interested in, only these are ever reported.</summary>
    [Required]
    [MaxLength(MaxUserIds)]
    public required Guid[] UserIds { get; init; }

    public const int MaxUserIds = 1000;
}
