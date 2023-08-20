using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Response;

public class ShockerLimits
{
    [Range(1, 100)]
    public required byte? Intensity { get; set; }
    [Range(300, 30000)]
    public required uint? Duration { get; set; }
}