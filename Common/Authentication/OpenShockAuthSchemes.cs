namespace OpenShock.Common.Authentication;

public static class OpenShockAuthSchemes
{
    public const string UserSessionCookie = "UserSessionCookie";
    public const string ApiToken = "ApiToken";
    public const string HubToken = "HubToken";

    public const string OAuthFlowScheme = "OAuthFlowCookie";
    public const string OAuthFlowCookieName = ".OpenShock.OAuthFlow";
    public const string DiscordScheme = "oauth-discord";
    public static readonly string[] OAuth2Schemes = [DiscordScheme];

    public const string UserSessionApiTokenCombo = $"{UserSessionCookie},{ApiToken}";
}