using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.SessionTokenCombo, Roles = "shockers.use")]
public class AuthenticatedSessionControllerBase : ShockLinkControllerBase
{
    public LinkUser CurrentUser = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}