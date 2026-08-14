using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OneOf.Types;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;

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
        var userReferenceService = HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();
        return userReferenceService.AuthReference.Match(
            (LoginSession _) => true, // We are in a session
            (ApiToken apiToken) => requiredType.IsAllowed(apiToken.Permissions),
            (None _) => throw new UnreachableException("User should be authenticated here")
        );
    }
}