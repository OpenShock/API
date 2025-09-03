using Microsoft.AspNetCore.Authentication;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Extensions;

public static class AuthenticationSchemeProviderExtensions
{
    public static async Task<string[]> GetAllOAuthSchemesAsync(this IAuthenticationSchemeProvider provider)
    {
        var schemes = await provider.GetAllSchemesAsync();

        return schemes
            .Select(scheme => scheme.Name)
            .Where(scheme => OpenShockAuthSchemes.OAuth2Schemes.Contains(scheme))
            .ToArray();
    }
    public static async Task<bool> IsSupportedOAuthScheme(this IAuthenticationSchemeProvider provider, string scheme)
    {
        if (!OpenShockAuthSchemes.OAuth2Schemes.Contains(scheme))
            return false;

        var schemes = await provider.GetAllSchemesAsync();

        return schemes.Any(s => s.Name == scheme);
    }
}
