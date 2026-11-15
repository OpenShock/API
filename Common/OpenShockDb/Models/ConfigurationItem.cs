namespace OpenShock.Common.OpenShockDb;

public sealed class ConfigurationItem
{
    public required string Name { get; init; }

    public required string Description { get; set; }

    public required ConfigurationValueType Type { get; init; }

    public required string Value { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime CreatedAt { get; init; }
}
