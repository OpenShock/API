namespace OpenShock.API.Models.Response;

public sealed class UserShareInfo
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }
    public required bool Paused { get; set; }
}