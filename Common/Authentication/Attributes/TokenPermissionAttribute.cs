using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Redis;

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
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();
        if (!tokenService.AuthReference.HasValue)
        {
            var error = AuthorizationError.UnknownError;
            context.Result = error.ToObjectResult(context.HttpContext);
            return;
        }
        
        // Use explicit out types so that if the interface changes, this code breaks
        if (tokenService.AuthReference.Value.TryPickT1(out ApiToken apiToken, out LoginSession _) && !_type.IsAllowed(apiToken.Permissions))
        {
            var problem = AuthorizationError.TokenPermissionMissing(_type, apiToken.Permissions);
            context.Result = problem.ToObjectResult(context.HttpContext);
        }
    }
}