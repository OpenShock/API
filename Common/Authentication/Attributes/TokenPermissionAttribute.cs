using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TokenPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly PermissionType _type;

    public TokenPermissionAttribute(PermissionType type)
    {
        _type = type;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        var problem = GetProblem(user);
        if (problem is not null)
        {
            context.Result = problem.ToObjectResult(context.HttpContext);
        }
    }

    private OpenShockProblem? GetProblem(ClaimsPrincipal user)
    {
        // A combined scheme authenticates every credential on the request, so a session cookie and an
        // API token can both be present. The token handler is the one that ends up stashing
        // the User item, so the token is what the action acts under and its permissions
        // bind - checking the session first here would let a cookie strip a restricted token's scope.
        if (user.HasOpenShockApiTokenIdentity())
        {
            var permissions = user.GetApiTokenPermissions();

            return _type.IsAllowed(permissions)
                ? null
                : AuthorizationError.TokenPermissionMissing(_type, permissions);
        }

        // A session on its own is not scoped by token permissions.
        if (user.HasOpenShockUserIdentity()) return null;

        // Neither scheme authenticated, so there are no permissions to check and this should not have
        // reached an authorized action.
        return AuthorizationError.UnknownError;
    }
}