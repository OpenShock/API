namespace OpenShock.Common.Options;

public sealed record DatabaseOptions(string Conn, bool SkipMigration = false, bool Debug = false);