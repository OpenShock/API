using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public class DeviceEdit
{
    [StringLength(32, MinimumLength = 1)]
    public required string Name { get; set; }
}