using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.API.OAuth;

public static class OAuthUtil
{
    /// <param name="provider"></param>
    /// <param name="flow"></param>
    /// <param name="activationToken">
    /// Token from the activation email, carried through the provider round trip so the handoff can
    /// activate the account the returned identity is linked to.
    /// </param>
    public static ChallengeResult StartOAuth(string provider, OAuthFlow flow, string? activationToken = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Items = { { OAuthConstants.ItemKeyFlowType, flow.ToString() } }
        };

        if (!string.IsNullOrEmpty(activationToken))
        {
            properties.Items[OAuthConstants.ItemKeyActivationToken] = activationToken;
        }

        return new ChallengeResult(provider, properties);
    }
}
