namespace OpenShock.API.OAuth;

public static class OAuthConstants
{
    public const string FlowScheme = "OAuthFlowCookie";
    public const string FlowCookieName = ".OpenShock.OAuthFlow";
    
    public const string DiscordScheme = "discord";
    public const string GoogleScheme  = "google";
    public const string TwitterScheme = "twitter";
    public static readonly string[] OAuth2Schemes = [DiscordScheme, GoogleScheme, TwitterScheme];
    
    public const string ItemKeyFlowType = ".FlowType";
    
    public const string ClaimEmailVerified = "openshock.oauth.email_verified";
    public const string ClaimDisplayName = "openshock.oauth.display_name";
}