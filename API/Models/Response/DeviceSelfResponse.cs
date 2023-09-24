namespace ShockLink.API.Models.Response;

public class DeviceSelfResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required IEnumerable<MinimalShocker> Shockers { get; set; }
}