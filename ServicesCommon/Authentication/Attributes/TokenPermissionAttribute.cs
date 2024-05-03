using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication.Services;
using OpenShock.ServicesCommon.Errors;

namespace OpenShock.ServicesCommon.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TokenPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly PermissionType _type;

    public TokenPermissionAttribute(PermissionType type)
    {
        _type = type;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tokenService = context.HttpContext.RequestServices.GetService<ITokenReferenceService<ApiToken>>();
        if (tokenService == null)
        {
            var error = AuthorizationError.UnknownError;
            context.Result = error.ToObjectResult(context.HttpContext);
            return;
        }

        if (tokenService.Token == null) return;
        
        if(_type.IsAllowed(tokenService.Token.Permissions)) return;
        
    var missingPermissionError =
            AuthorizationError.TokenPermissionMissing(_type, tokenService.Token.Permissions);
        context.Result = missingPermissionError.ToObjectResult(context.HttpContext);
    }
}