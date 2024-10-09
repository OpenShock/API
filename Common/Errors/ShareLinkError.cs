using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ShareLinkError
{
    public static OpenShockProblem ShareLinkNotFound => new("ShareLink.NotFound", "Share link not found", HttpStatusCode.NotFound);
    
    // Add shocker errors
    public static OpenShockProblem ShockerAlreadyInShareLink => new("ShareLink.ShockerAlreadyInShareLink", "Shocker already exists in share link", HttpStatusCode.Conflict);
    
    // Remove shocker errors
    public static OpenShockProblem ShockerNotInShareLink => new("ShareLink.ShockerNotInShareLink", "Shocker does not exist in share link", HttpStatusCode.NotFound);
}