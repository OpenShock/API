using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using OpenShock.Common.Authentication;

namespace OpenShock.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    private static readonly Func<ClaimsIdentity, bool> UserClaimPredicate = x => x is
        { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie };
    
    public static bool HasOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.Any(UserClaimPredicate);
    public static ClaimsIdentity GetOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.Single(UserClaimPredicate);
    public static ClaimsIdentity? TryGetOpenShockUserIdentity(this ClaimsPrincipal principal) => principal.Identities.SingleOrDefault(UserClaimPredicate);
    public static string GetClaimValue(this ClaimsIdentity identity, string claimType) => identity.Claims.Single(x => x.Type == claimType).Value;
    public static Guid GetClaimValueAsGuid(this ClaimsIdentity identity, string claimType) => Guid.Parse(GetClaimValue(identity, claimType));
}
