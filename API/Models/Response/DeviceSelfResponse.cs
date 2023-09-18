namespace ShockLink.API.Models.Response;

public class DeviceSelfResponse
{
    public required IEnumerable<MinimalShocker> Shockers { get; set; }
}