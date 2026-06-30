namespace OpenShock.Common.Utils;

/// <summary>
/// Marks an enum as a Postgres native enum type so it is automatically registered
/// with the EF Core model via <see cref="OpenShockDb.NpgsqlEnumExtensions.RegisterPgEnums"/>.
/// </summary>
/// <param name="name">
/// Explicit Postgres type name. When omitted, the C# type name is converted to snake_case.
/// </param>
/// <param name="schema">Postgres schema; defaults to the model's default schema when null.</param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class PgEnumAttribute(string? name = null, string? schema = null) : Attribute
{
    public string? Name { get; } = name;
    public string? Schema { get; } = schema;
}
