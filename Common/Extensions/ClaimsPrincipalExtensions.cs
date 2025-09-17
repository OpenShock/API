using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using OpenShock.Common.Authentication;

namespace OpenShock.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool HasOpenShockUserIdentity(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        
        foreach (var ident in principal.Identities)
        {
            if (ident is { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie })
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool TryGetOpenShockUserIdentity(this ClaimsPrincipal principal, [NotNullWhen(true)] out ClaimsIdentity? identity)
    {
        ArgumentNullException.ThrowIfNull(principal);
        
        foreach (var ident in principal.Identities)
        {
            if (ident is { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie })
            {
                identity = ident;
                return true;
            }
        }

        identity = null;
        return false;
    }

    public static bool TryGetAuthenticatedOpenShockUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        
        if (!principal.TryGetOpenShockUserIdentity(out var identity))
        {
            userId = Guid.Empty;
            return false;
        }
        
        var idStr = identity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idStr))
        {
            userId = Guid.Empty;
            return false;
        }

        return Guid.TryParse(idStr, out userId);
    }
}
