namespace OpenShock.Common.Options;

public sealed class DatabaseOptions
{
    public required string Conn { get; init; }
    public bool SkipMigration { get; init; } = false;
    public bool Debug { get; init; } = false;
}