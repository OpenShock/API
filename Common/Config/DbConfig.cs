using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Config;

public sealed class DbConfig
{
    [Required(AllowEmptyStrings = false)] public required string Conn { get; init; }
    public bool SkipMigration { get; init; } = false;
    public bool Debug { get; init; } = false;
}