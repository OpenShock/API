using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.Authentication.ControllerBase;

namespace OpenShock.API.Controller;

[ApiController]
[Route("/{version:apiVersion}/test")]
[TokenOnly, TokenPermission(PermissionType.Shockers_Edit)]
public class TestController : AuthenticatedSessionControllerBase
{
    [HttpGet]
    public async Task Test()
    {
        await Task.CompletedTask;
    }
}