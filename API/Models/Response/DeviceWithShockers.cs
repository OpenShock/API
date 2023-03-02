namespace ShockLink.API.Models.Response;

public class DeviceWithShockers
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}