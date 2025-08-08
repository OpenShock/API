using OpenShock.Common.OpenShockDb;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Request DTO for updating a configuration item.
/// Either <see cref="Description"/> or <see cref="Value"/> (or both) may be provided.
/// If both are null, no update action is performed.
/// </summary>
public sealed class ConfigurationUpdateItemRequest
{
    /// <summary>
    /// The name of the configuration item to update.
    /// Must consist only of uppercase letters and underscores.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// (Optional) New description for the configuration item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// (Optional) New value for the configuration item.
    /// Must match the format expected by its existing <see cref="ConfigurationValueType"/>.
    /// </summary>
    public string? Value { get; set; }
}