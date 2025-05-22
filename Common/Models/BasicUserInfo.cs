namespace OpenShock.Common.Models;

public sealed class BasicUserInfo
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Uri Image { get; set; }
}