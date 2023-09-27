using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.ServicesCommon.Authentication;

[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
public class AuthenticatedDeviceControllerBase : OpenShockControllerBase
{
    public Device CurrentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Device>>().CurrentClient;
        base.OnActionExecuting(context);
    }
}