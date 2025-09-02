namespace OpenShock.Common.Constants;

public static class AuthConstants
{
    public const string UserSessionCookieName = "openShockSession";
    public const string UserSessionHeaderName = "OpenShockSession";
    public const string ApiTokenHeaderName = "OpenShockToken";
    public const string HubTokenHeaderName = "DeviceToken";

    public const string DiscordScheme = "discord";
    public static readonly string[] OAuth2Schemes = [DiscordScheme];

    public const int GeneratedTokenLength = 32;
    public const int ApiTokenLength = 64;
}
