namespace OpenShock.API.Constants;

public static class OAuthConstants
{
    public const string FlowScheme = "OAuthFlowCookie";
    public const string FlowCookieName = ".OpenShock.OAuthFlow";
    
    public const string DiscordScheme = "discord";
    public static readonly string[] OAuth2Schemes = [DiscordScheme];
    
    public const string ItemKeyFlowType = "openshock.oauth.flow_type";
    
    public const string ClaimEmailVerified = "openshock.oauth.email_verified";
    public const string ClaimDisplayName = "openshock.oauth.display_name";
}