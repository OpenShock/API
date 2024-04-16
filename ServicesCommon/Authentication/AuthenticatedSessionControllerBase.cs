using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo, Roles = "shockers.use")]
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