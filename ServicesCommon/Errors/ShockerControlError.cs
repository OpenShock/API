using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class ShockerControlError
{
    public static ShockerControlProblem ShockerControlNotFound(Guid shockerId) =>
        new("Shocker.Control.NotFound", "Shocker control not found", shockerId, HttpStatusCode.NotFound);
    public static ShockerControlProblem ShockerControlPaused(Guid shockerId) =>
        new("Shocker.Control.Paused", "Shocker is paused", shockerId, HttpStatusCode.PreconditionFailed);
    public static ShockerControlProblem ShockerControlNoPermission(Guid shockerId) =>
        new("Shocker.Control.NoPermission", "You don't have permission to control this shocker",  shockerId, HttpStatusCode.Forbidden);
}