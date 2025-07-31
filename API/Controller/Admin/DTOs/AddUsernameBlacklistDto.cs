using MatchTypeEnum = OpenShock.Common.OpenShockDb.MatchType;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddUsernameBlacklistDto
{
    public required string Value { get; init; }
    public required MatchTypeEnum MatchType { get; init; }
}
