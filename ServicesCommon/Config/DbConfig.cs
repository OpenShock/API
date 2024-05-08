using System.ComponentModel.DataAnnotations;

namespace OpenShock.ServicesCommon.Config;

public sealed class DbConfig
{
    [Required(AllowEmptyStrings = true)] public required string Conn { get; init; }
    public bool SkipMigration { get; init; } = false;
    public bool Debug { get; init; } = false;
}