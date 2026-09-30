// ReSharper disable InconsistentNaming

using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "password_encryption_type")]
public enum PasswordHashingAlgorithm
{
    Unknown = -1,
    [PgName("bcrypt_enhanced")] BCrypt = 0,
    [PgName("pbkdf2")] PBKDF2 = 1
}
