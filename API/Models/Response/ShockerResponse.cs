using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class ShockerResponse
{
    public required Guid Id { get; set; }
    public required ushort RfId { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required ShockerModelType Model { get; set; }
}