using NpgsqlTypes;

namespace OpenShock.Common.Models;

public enum PasswordEncryptionType
{
    [PgName("pbkdf2")] Pbkdf2,
    [PgName("bcrypt_enhanced")] BcryptEnhanced
}