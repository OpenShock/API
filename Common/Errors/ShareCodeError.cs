using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ShareCodeError
{
    public static OpenShockProblem ShareCodeNotFound => new("ShareCode.NotFound", "Share code not found", HttpStatusCode.NotFound);
    
    public static OpenShockProblem CantLinkOwnShareCode => new("ShareCode.CantLinkOwnShareCode", "Cant link your own share code to your account", HttpStatusCode.BadRequest);

    public static OpenShockProblem ShockerAlreadyLinked => new("ShareCode.AlreadyLinked",
        "Shocker already linked to your account", HttpStatusCode.BadRequest);
}