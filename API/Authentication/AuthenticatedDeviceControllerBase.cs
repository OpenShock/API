using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using ShockLink.API.Controller;

namespace ShockLink.API.Authentication;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.DeviceToken)]
public class AuthenticatedDeviceControllerBase : ShockLinkControllerBase
{
    public Common.ShockLinkDb.Device CurrentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Common.ShockLinkDb.Device>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}