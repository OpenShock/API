using System.ComponentModel.DataAnnotations;
using ShockLink.Common.Models;

namespace ShockLink.API.Models.Requests;

public class Control
{
    public required IEnumerable<Guid> Shockers { get; set; }
    public required ControlType Type { get; set; }
    [Range(1, 100)]
    public required byte Intensity { get; set; }
    [Range(300, 30000)]
    public required uint Duration { get; set; }
}