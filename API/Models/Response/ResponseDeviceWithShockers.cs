namespace ShockLink.API.Models.Response;

public class ResponseDeviceWithShockers : ResponseDevice
{
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}