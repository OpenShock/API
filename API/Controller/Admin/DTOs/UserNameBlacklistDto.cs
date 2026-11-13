using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class UserNameBlacklistDto
{
    public required Guid Id { get; init; }

    public required string Value { get; init; } = null!;

    public required MatchTypeEnum MatchType { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
