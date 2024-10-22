using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class HubEditRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(32, MinimumLength = 1)]
    public required string Name { get; set; }
}