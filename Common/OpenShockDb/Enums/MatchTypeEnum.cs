using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "match_type_enum")]
public enum MatchTypeEnum
{
    [PgName("exact")] Exact = 0,
    [PgName("contains")] Contains = 1,
}
