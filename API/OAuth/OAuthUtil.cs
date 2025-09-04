using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Constants;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Utils;

public static class OAuthUtil
{
    public static ChallengeResult StartOAuth(string provider, string flow)
    {
        return new ChallengeResult(provider, new AuthenticationProperties
        {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Items = { { OAuthConstants.ItemKeyFlowType, flow } }
        });
    }
}