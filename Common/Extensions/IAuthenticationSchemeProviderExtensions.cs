using Microsoft.AspNetCore.Authentication;
using OpenShock.Common.Constants;
using System.Linq;
using OpenShock.Common.Authentication;

namespace OpenShock.Common.Extensions;

public static class IAuthenticationSchemeProviderExtensions
{
    public static async Task<string[]> GetOAuthSchemeNamesAsync(this IAuthenticationSchemeProvider provider)
    {
        var allSchemes = await provider.GetAllSchemesAsync();

        return allSchemes
            .Where(scheme => OpenShockAuthSchemes.OAuth2Schemes.Contains(scheme.Name))
            .Select(scheme => scheme.Name)
            .ToArray();
    }
    public static async Task<bool> IsSupportedOAuthProviderAsync(this IAuthenticationSchemeProvider provider, string scheme)
    {
        foreach (var supportedScheme in await provider.GetOAuthSchemeNamesAsync())
        {
            if (string.Equals(scheme, supportedScheme, StringComparison.InvariantCultureIgnoreCase)) return true;
        }

        return false;
    }
}
