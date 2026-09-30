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
        // Session auth is not scoped by API token permissions.
        if (User.HasOpenShockUserIdentity()) return true;

        return requiredType.IsAllowed(User.GetApiTokenPermissions());
    }
}