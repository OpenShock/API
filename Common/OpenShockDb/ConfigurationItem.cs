namespace OpenShock.Common.OpenShockDb;

public enum ConfigurationValueType
{
    String,
    Bool,
    Int,
    Float,
    Json
}

public class ConfigurationItem
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required ConfigurationValueType Type { get; set; }
    public required string Value { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
