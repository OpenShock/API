using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class CreateShareRequest
{
    [MaxLength(128)] // Hard limit
    public required IEnumerable<CreateShockerShareWithId> Shockers { get; set; }
    public Guid? User { get; set; } = null;
}

