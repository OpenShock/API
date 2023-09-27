using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo, Roles = "shockers.use")]
public class AuthenticatedSessionControllerBase : OpenShockControllerBase
{
    public LinkUser CurrentUser = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}