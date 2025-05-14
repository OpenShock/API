namespace OpenShock.Common.Authentication;

public static class OpenShockAuthSchemas
{
    public const string UserSessionCookie = "UserSessionCookie";
    public const string ApiToken = "ApiToken";
    public const string HubToken = "HubToken";

    public const string UserSessionApiTokenCombo = $"{UserSessionCookie},{ApiToken}";
}