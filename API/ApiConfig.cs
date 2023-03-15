namespace ShockLink.API;

public static class ApiConfig
{
    public static readonly string Db = GetVarOrDefault("DB");
    public static readonly string RedisHost = GetVarOrDefault("REDIS_HOST");
    public static readonly string RedisPassword = GetVarOrDefault("REDIS_PASSWORD", "");
    public static readonly string CloudflareAccountId = GetVarOrDefault("CF_ACC_ID");
    public static readonly string CloudflareImagesKey = GetVarOrDefault("CF_IMG_KEY");

    private static string GetVarOrDefault(string variableName, string? defaultValue = null)
    {
        var var = Environment.GetEnvironmentVariable(variableName);
        if (var != null) return var;
        if (defaultValue == null)
            throw new ArgumentNullException(variableName,
                "Environment variable is null, and no default value is provided");
        return defaultValue;
    }
}