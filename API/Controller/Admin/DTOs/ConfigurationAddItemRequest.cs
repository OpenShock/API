using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Request DTO for adding a new configuration item.
/// </summary>
public sealed class ConfigurationAddItemRequest
{
    /// <summary>
    /// The unique name of the configuration item.
    /// Must consist of uppercase letters and underscores only.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A human-readable description of the configuration item’s purpose.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The data type of the configuration value.
    /// </summary>
    public required ConfigurationValueType Type { get; init; }

    /// <summary>
    /// The initial value for the configuration item, serialized as a string.
    /// Must conform to the format required by <see cref="Type"/>.
    /// </summary>
    public required string Value { get; init; }
}