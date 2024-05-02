using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.ServicesCommon.Authentication.Services;

namespace OpenShock.ServicesCommon.Authentication.ControllerBase;

[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo)]
public class AuthenticatedSessionControllerBase : OpenShockControllerBase, IActionFilter
{
    public LinkUser CurrentUser = null!;

    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>().CurrentClient;
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}