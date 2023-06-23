namespace ShockLink.API;

public static class ApiConfig
{
    public static readonly string FrontendBaseUrl = GetVarOrDefault("FRONTEND_BASE_URL");
    public static readonly string Db = GetVarOrDefault("DB");
    public static readonly string RedisHost = GetVarOrDefault("REDIS_HOST");
    public static readonly string RedisPassword = GetVarOrDefault("REDIS_PASSWORD", "");
    
    public static class Cloudflare
    {
        public static readonly string AccountId = GetVarOrDefault("CF_ACC_ID");
        public static readonly string ImagesKey = GetVarOrDefault("CF_IMG_KEY");
        public static readonly string ImagesUrl = GetVarOrDefault("CF_IMG_URL");
    }
    
    public static class Mailjet
    {
        public static readonly string Key = GetVarOrDefault("MAILJET_KEY");
        public static readonly string Secret = GetVarOrDefault("MAILJET_SECRET");
    }
    
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