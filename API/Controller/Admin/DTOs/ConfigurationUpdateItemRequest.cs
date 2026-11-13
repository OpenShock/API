using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;

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
    [StringLength(ApiHardLimits.ConfigurationNameMaxLength)]
    public required string Name { get; init; }

    /// <summary>
    /// (Optional) New description for the configuration item.
    /// </summary>
    [StringLength(ApiHardLimits.ConfigurationDescriptionMaxLength)]
    public string? Description { get; init; }

    /// <summary>
    /// (Optional) New value for the configuration item.
    /// Must match the format expected by its existing <see cref="ConfigurationValueType"/>.
    /// </summary>
    [StringLength(ApiHardLimits.ConfigurationValueMaxLength)]
    public string? Value { get; init; }
}