using System.Diagnostics.CodeAnalysis;
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
            RedirectUri = $"/1/oauth/{provider}/handoff"
        };

        SetOAuthFlow(properties, flow);

        if (flow == OAuthFlow.LoginOrCreate)
        {
            SetActivationToken(properties, activationToken);
        }

        return new ChallengeResult(provider, properties);
    }

    /// <summary>Reads the flow that was started. False when the properties carry none, or one that is not known.</summary>
    public static bool TryGetOAuthFlow(AuthenticationProperties properties, out OAuthFlow flow)
        => Enum.TryParse(properties.GetString(OAuthConstants.ItemKeyFlowType), ignoreCase: true, out flow);

    public static void SetOAuthFlow(AuthenticationProperties properties, OAuthFlow flow)
        => properties.SetString(OAuthConstants.ItemKeyFlowType, flow.ToString());

    /// <summary>
    /// Reads the token from the activation email that the flow carries, so the handoff can activate the
    /// account the returned identity is linked to. False when there is none.
    /// </summary>
    public static bool TryGetActivationToken(AuthenticationProperties properties, [NotNullWhen(true)] out string? activationToken)
    {
        activationToken = properties.GetString(OAuthConstants.ItemKeyActivationToken);
        if (!string.IsNullOrEmpty(activationToken)) return true;

        activationToken = null;
        return false;
    }

    /// <summary>Sets the activation token the flow carries. Null or empty removes it.</summary>
    public static void SetActivationToken(AuthenticationProperties properties, string? activationToken)
        => properties.SetString(OAuthConstants.ItemKeyActivationToken, string.IsNullOrEmpty(activationToken) ? null : activationToken);
}
