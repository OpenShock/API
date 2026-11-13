namespace OpenShock.Common.Models;

public sealed class BasicUserInfo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Uri Image { get; init; }
}
