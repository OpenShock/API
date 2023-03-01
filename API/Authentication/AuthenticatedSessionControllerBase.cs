using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using ShockLink.API.Controller;

namespace ShockLink.API.Authentication;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.SessionTokenCombo)]
public class AuthenticatedSessionControllerBase : ShockLinkControllerBase
{
    public LinkUser CurrentUser = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}