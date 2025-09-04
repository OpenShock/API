using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;

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
        var permissions = context.HttpContext.User.Claims.Where(x => x.Type == OpenShockAuthClaims.ApiTokenPermission).Select(x => x.Value).ToArray();
        
        if (!permissions.Contains(_type.ToString()))
        {
            var problem = AuthorizationError.TokenPermissionMissing(_type, permissions.Select(Enum.Parse<PermissionType>).ToArray());
            context.Result = problem.ToObjectResult(context.HttpContext);
        }
    }
}