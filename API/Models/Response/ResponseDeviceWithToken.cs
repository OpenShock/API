namespace ShockLink.API.Models.Response;

public class ResponseDeviceWithToken : ResponseDevice
{
    public required string Token { get; set; }
}