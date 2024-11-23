// ReSharper disable InconsistentNaming
namespace OpenShock.Common.Models;

public enum PasswordHashingAlgorithm
{
    Unknown = -1,
    BCrypt = 0,
    PBKDF2 = 1,
};