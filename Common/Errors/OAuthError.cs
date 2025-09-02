using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class OAuthError
{
    public static OpenShockProblem ProviderNotSupported => new OpenShockProblem(
        "OAuth.Provider.NotSupported", "This OAuth provider is not supported", HttpStatusCode.Forbidden);
}