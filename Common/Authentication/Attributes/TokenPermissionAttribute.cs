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

        // A session is not scoped by token permissions; anything that is neither a session nor an
        // API token has no permissions to check and should not have reached an authorized action.
        if (user.HasOpenShockUserIdentity()) return;

        var problem = GetProblem(user);
        if (problem is not null)
        {
            context.Result = problem.ToObjectResult(context.HttpContext);
        }
    }

    private OpenShockProblem? GetProblem(ClaimsPrincipal user)
    {
        if (!user.HasOpenShockApiTokenIdentity()) return AuthorizationError.UnknownError;

        var permissions = user.GetApiTokenPermissions();

        return _type.IsAllowed(permissions)
            ? null
            : AuthorizationError.TokenPermissionMissing(_type, permissions);
    }
}