using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public class MinimalShocker
{
    public required Guid Id { get; init; }
    public required ushort RfId { get; init; }
    public required ShockerModelType Model { get; init; }
}

public class ShockerResponse : MinimalShocker
{
    public required string Name { get; init; }
    public required bool IsPaused { get; init; }
    public required DateTime CreatedOn { get; init; }
}