using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.API.OAuth;

public static class OAuthUtil
{
    /// <param name="provider"></param>
    /// <param name="flow"></param>
    /// <param name="activationToken">
    /// Token from the activation email, carried through the provider round trip so the handoff can
    /// activate the account the returned identity is linked to. Only a <see cref="OAuthFlow.LoginOrCreate"/>
    /// flow carries it: linking is for an account that is already signed in and never activates anything.
    /// </param>
    public static ChallengeResult StartOAuth(string provider, OAuthFlow flow, string? activationToken = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Flow = flow
        };

        if (flow == OAuthFlow.LoginOrCreate)
        {
            properties.ActivationToken = activationToken;
        }

        return new ChallengeResult(provider, properties);
    }
}
