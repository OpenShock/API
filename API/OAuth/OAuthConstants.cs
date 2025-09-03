namespace OpenShock.API.OAuth;

public static class OAuthConstants
{
    public const string LoginOrCreate = "login-or-create";
    public const string LinkFlow = "link";
    
    public static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    public const string StateCachePrefix = "oauth:state:";
}