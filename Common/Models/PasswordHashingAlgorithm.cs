// ReSharper disable InconsistentNaming
using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

[PgEnum("password_encryption_type")]
public enum PasswordHashingAlgorithm
{
    Unknown = -1,
    [PgName("bcrypt_enhanced")] BCrypt = 0,
    [PgName("pbkdf2")] PBKDF2 = 1,
};
