using System.Net;
using OpenShock.Common.Problems;
using OpenShock.Common.Problems.CustomProblems;

namespace OpenShock.Common.Errors;

public static class ShareError
{
    public static OpenShockProblem ShareCreateCannotShareWithSelf => new("Share.Create.CannotShareWithSelf", "You cannot share something with yourself", HttpStatusCode.BadRequest);
    public static ShockersNotFoundProblem ShareCreateShockerNotFound(IEnumerable<Guid> missingShockers) => new("Share.Create.ShockerNotFound", "One or multiple of the provided shocker's were not found or do not belong to you", missingShockers, HttpStatusCode.NotFound);
    
    public static OpenShockProblem ShareGetNoShares => new("Share.Get.NoShares", "You have no shares with the specified user, or the user doesnt exist", HttpStatusCode.NotFound);
}