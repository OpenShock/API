namespace OpenShock.API.Models.Response;

public sealed class UserShareInfo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedOn { get; init; }
    public required ShockerPermissions Permissions { get; init; }
    public required ShockerLimits Limits { get; init; }
    public required bool Paused { get; init; }
}