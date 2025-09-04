using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.API.OAuth;

public static class OAuthUtil
{
    public static ChallengeResult StartOAuth(string provider, OAuthFlow flow)
    {
        return new ChallengeResult(provider, new AuthenticationProperties
        {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Items = { { OAuthConstants.ItemKeyFlowType, flow.ToString() } }
        });
    }
}