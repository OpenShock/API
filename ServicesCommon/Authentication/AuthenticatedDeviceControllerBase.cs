using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.ShockLinkDb;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.DeviceToken)]
public class AuthenticatedDeviceControllerBase : ShockLinkControllerBase
{
    public Device CurrentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Device>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}