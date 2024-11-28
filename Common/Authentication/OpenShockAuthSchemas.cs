using OpenShock.Common.Constants;

namespace OpenShock.Common.Authentication;

public static class OpenShockAuthSchemas
{
    public const string UserSessionCookie = "user-session-cookie";
    public const string ApiToken = "api-token";
    public const string HubToken = "hub-token";

    public const string UserSessionApiTokenCombo = $"{UserSessionCookie},{ApiToken}";
}