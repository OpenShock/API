namespace OpenShock.Common.Options;

public sealed class DatabaseOptions
{
    public required string Conn { get; init; }
    public required bool SkipMigration { get; init; }
    public required bool Debug { get; init; }
}