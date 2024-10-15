using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TokenOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userReferenceService = context.HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();

        if (userReferenceService.AuthReference == null)
        {
            var error = AuthorizationError.UnknownError;
            context.Result = error.ToObjectResult(context.HttpContext);
            return;
        }
        
        if (!userReferenceService.AuthReference.Value.IsT1) context.Result = AuthorizationError.TokenOnly.ToObjectResult(context.HttpContext);
    }
}