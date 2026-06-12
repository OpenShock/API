using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum]
public enum ConfigurationValueType
{
    [PgName("string")] String,
    [PgName("bool")] Bool,
    [PgName("int")] Int,
    [PgName("float")] Float,
    [PgName("json")] Json,
}

public sealed class ConfigurationItem
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required ConfigurationValueType Type { get; set; }
    public required string Value { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
