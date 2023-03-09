namespace ShockLink.API.Models.Requests;

public class CreateShareCode
{
    public bool PermSound { get; set; }
    public bool PermVibrate { get; set; }
    public bool PermShock { get; set; }
    
    public uint? LimitDuration { get; set; }
    public byte? LimitIntensity { get; set; }
}