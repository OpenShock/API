namespace ShockLink.API.Models.Response;

public class DeviceWithShockers : Device
{
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}