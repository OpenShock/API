using Microsoft.AspNetCore.Authentication;

namespace OpenShock.API.OAuth;

/// <summary>
/// What an OAuth flow carries through the provider round trip, as properties on
/// <see cref="AuthenticationProperties"/> instead of string keys into its items.
/// </summary>
public static class OAuthFlowPropertiesExtensions
{
    extension(AuthenticationProperties properties)
    {
        /// <summary>The flow that was started, or null when the properties carry none (or one that is not known).</summary>
        public OAuthFlow? Flow
        {
            get => Enum.TryParse<OAuthFlow>(properties.GetString(OAuthConstants.ItemKeyFlowType), ignoreCase: true, out var flow) ? flow : null;
            set => properties.SetString(OAuthConstants.ItemKeyFlowType, value?.ToString());
        }

        /// <summary>
        /// Token from the activation email, carried along so the handoff can activate the account the returned
        /// identity is linked to. Null when there is none; setting null or an empty value removes it.
        /// </summary>
        public string? ActivationToken
        {
            get => properties.GetString(OAuthConstants.ItemKeyActivationToken) is { Length: > 0 } token ? token : null;
            set => properties.SetString(OAuthConstants.ItemKeyActivationToken, string.IsNullOrEmpty(value) ? null : value);
        }
    }
}
