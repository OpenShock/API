using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Authentication.ControllerBase;

public class AuthenticatedSessionControllerBase : OpenShockControllerBase, IActionFilter
{
    public User CurrentUser = null!;

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
    public bool IsAllowed(PermissionType requiredType)
    {
        var userReferenceService = HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();

        if (userReferenceService.AuthReference == null) throw new Exception("UserReferenceService.AuthReference is null, this should not happen");

        if (userReferenceService.AuthReference.Value.IsT0) return true; // We are in a session

        return requiredType.IsAllowed(userReferenceService.AuthReference.Value.AsT1.Permissions);
    }
}