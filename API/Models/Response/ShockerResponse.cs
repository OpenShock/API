using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class MinimalShocker
{
    public required Guid Id { get; set; }
    public required ushort RfId { get; set; }
    public required ShockerModelType Model { get; set; }
}

public class ShockerResponse : MinimalShocker
{
    public required string Name { get; set; }
    public required bool IsPaused { get; set; }
    public required DateTime CreatedOn { get; set; }
}