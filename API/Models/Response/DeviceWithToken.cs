namespace ShockLink.API.Models.Response;

public class DeviceWithToken : Device
{
    public required string Token { get; set; }
}