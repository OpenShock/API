using System.Security.Claims;
using OpenShock.Common.Authentication;
using OpenShock.Common.Models;

namespace OpenShock.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    private static readonly Func<ClaimsIdentity, bool> HubIdentityPredicate = x => x is
        { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.HubToken };
    private static readonly Func<ClaimsIdentity, bool> UserIdentityPredicate = x => x is
        { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie };
    
    public static bool HasOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.Any(UserIdentityPredicate);
    public static ClaimsIdentity GetOpenShockHubIdentity(this ClaimsPrincipal principal) => principal.Identities.Single(HubIdentityPredicate);
    public static ClaimsIdentity GetOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.Single(UserIdentityPredicate);
    public static ClaimsIdentity? TryGetOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.SingleOrDefault(UserIdentityPredicate);
    public static bool TryGetClaimValueAsGuid(this ClaimsPrincipal principal, string claimType, out Guid guid)
    {
        var claim = principal.Claims.FirstOrDefault(x => x.Type == claimType);
        if (claim is null)
        {
            guid = Guid.Empty;
            return false;
        }
        
        guid = Guid.Parse(claim.Value);
        return true;
    }
    
    public static string GetClaimValue(this ClaimsIdentity identity, string claimType) => identity.Claims.Single(x => x.Type == claimType).Value;
    public static Guid GetClaimValueAsGuid(this ClaimsIdentity identity, string claimType) => Guid.Parse(GetClaimValue(identity, claimType));

    public static IEnumerable<RoleType> GetRoles(this ClaimsIdentity identity)
    {
        return identity.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => Enum.Parse<RoleType>(x.Value));
    }

    public static bool IsAdminOrSystem(this ClaimsIdentity identity)
    {
        return GetRoles(identity).Any(x => x is RoleType.Admin or RoleType.System);
    }
}
