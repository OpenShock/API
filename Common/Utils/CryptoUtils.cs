using System.Security.Cryptography;

namespace OpenShock.API.Utils;

public static class CryptoUtils
{
    public static string RandomString(int length) => RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890", length);
}