using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Redis;
using OpenShock.Common.Results;

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

    private static OpenShockProblem? LoginSessionMatch(LoginSession _)
    {
        return null;
    }

    private OpenShockProblem? ApiTokenMatch(ApiToken apiToken)
    {
        if (!_type.IsAllowed(apiToken.Permissions))
        {
            return AuthorizationError.TokenPermissionMissing(_type, apiToken.Permissions);
        }
        
        return null;
    }

    private static OpenShockProblem? NoneMatch(None _)
    {
        return AuthorizationError.UnknownError;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userReference = context.HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();

        var problem = userReference.AuthReference switch
        {
            LoginSession loginSession => LoginSessionMatch(loginSession),
            ApiToken apiToken => ApiTokenMatch(apiToken),
            None none => NoneMatch(none)
        };

        if (problem is not null)
        {
            context.Result = problem.ToObjectResult(context.HttpContext);
        }
    }
}