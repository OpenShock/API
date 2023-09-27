using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using ShockLink.API.Authentication;
using ShockLink.API.Controller;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.DeviceToken)]
public class AuthenticatedDeviceControllerBase : ShockLinkControllerBase
{
    public ShockLink.Common.ShockLinkDb.Device CurrentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<ShockLink.Common.ShockLinkDb.Device>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}