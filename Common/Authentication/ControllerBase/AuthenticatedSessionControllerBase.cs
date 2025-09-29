using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Authentication.ControllerBase;

public class AuthenticatedSessionControllerBase : OpenShockControllerBase, IActionFilter
{
    protected User CurrentUser = null!;

    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = HttpContext.Items["User"] as User ?? throw new Exception("User not found");
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    [NonAction]
    protected bool IsAllowed(PermissionType requiredType)
    {
        if (User.Identities.Any(x => x is { Name: OpenShockAuthSchemes.UserSessionCookie, IsAuthenticated: true }))
        {
            return true;
        }

        var permissions = User.Claims
            .Where(c => c.Type == OpenShockAuthClaims.ApiTokenPermission)
            .Select(c => Enum.Parse<PermissionType>(c.Value))
            .ToArray();
        
        return requiredType.IsAllowed(permissions);
    }
}