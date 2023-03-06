using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class DeviceEdit
{
    [StringLength(32, MinimumLength = 1)]
    public required string Name { get; set; }
}