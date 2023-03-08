using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class NewShocker
{
    [StringLength(48, MinimumLength = 1)]
    public required string Name { get; set; }
    public required ushort RfId { get; set; }
    public required Guid Device { get; set; }
}