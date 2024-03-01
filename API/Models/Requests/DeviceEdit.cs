using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class DeviceEdit
{
    [StringLength(32, MinimumLength = 1)]
    public required string Name { get; set; }
}