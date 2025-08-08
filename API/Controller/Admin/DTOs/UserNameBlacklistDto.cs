using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class UserNameBlacklistDto
{
    public required Guid Id { get; set; }

    public required string Value { get; set; } = null!;

    public required MatchTypeEnum MatchType { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}