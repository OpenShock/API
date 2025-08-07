using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Data transfer object representing a configuration item.
/// </summary>
public sealed class ConfigurationItemDto
{
    /// <summary>
    /// The unique name of the configuration item.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A human-readable description of what this configuration item controls.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The data type of the configuration value.
    /// </summary>
    public required ConfigurationValueType Type { get; init; }

    /// <summary>
    /// The current value of the configuration item, serialized as a string.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The timestamp when the configuration was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// The timestamp when the configuration was originally created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}