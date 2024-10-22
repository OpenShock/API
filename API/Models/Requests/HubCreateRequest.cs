using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class HubCreateRequest
{
    [StringLength(32, MinimumLength = 1)]
    public string? Name { get; set; } = null;
}