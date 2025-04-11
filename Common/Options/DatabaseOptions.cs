using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "OpenShock:DB";

    [Required(AllowEmptyStrings = false)]
    public required string Conn { get; init; }
    public bool SkipMigration { get; init; } = false;
    public bool Debug { get; init; } = false;
}

[OptionsValidator]
public partial class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
}