using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class PublicShareError
{
    public static OpenShockProblem PublicShareNotFound => new("ShareLink.NotFound", "Public share not found", HttpStatusCode.NotFound);
    
    // Add shocker errors
    public static OpenShockProblem ShockerAlreadyInPublicShare => new("ShareLink.ShockerAlreadyInShareLink", "Shocker already exists in public share", HttpStatusCode.Conflict);
    
    // Remove shocker errors
    public static OpenShockProblem ShockerNotInPublicShare => new("ShareLink.ShockerNotInShareLink", "Shocker does not exist in public share", HttpStatusCode.NotFound);
    
    // Create share errors
    public static OpenShockProblem PublicShareExpiryDateInPast => new("ShareLink.ExpiryDateInPast", "Expiry date cannot be in the past", HttpStatusCode.BadRequest);
}