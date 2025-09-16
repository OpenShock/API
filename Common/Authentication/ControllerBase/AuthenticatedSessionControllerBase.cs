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
        CurrentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<User>>().CurrentClient;
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    [NonAction]
    protected bool IsAllowed(PermissionType requiredType)
    {
        var userReferenceService = HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();

        if (userReferenceService.AuthReference is null) throw new Exception("UserReferenceService.AuthReference is null, this should not happen");

        return userReferenceService.AuthReference.Value.Match(
            loginSession => true, // We are in a session
            apiToken => requiredType.IsAllowed(apiToken.Permissions)
        );
    }
}