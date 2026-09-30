using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Results;

namespace OpenShock.Common.Authentication.ControllerBase;

public class AuthenticatedSessionControllerBase : OpenShockControllerBase, IActionFilter
{
    protected User CurrentUser = null!;

    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = GetRequiredItem<User>();
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    [NonAction]
    protected bool IsAllowed(PermissionType requiredType)
    {
        // Checked before the session for the same reason as TokenPermissionAttribute: when both
        // credentials are on the request, the token is the one CurrentUser came from.
        if (User.HasOpenShockApiTokenIdentity()) return requiredType.IsAllowed(User.GetApiTokenPermissions());

        // Session auth on its own is not scoped by API token permissions.
        if (User.HasOpenShockUserIdentity()) return true;

        throw new UnreachableException("User should be authenticated here");
    }
}