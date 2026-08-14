using System.Net;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.Errors;

public static class GoneError
{
    /// <summary>
    /// Returned by retired endpoints. Points the caller at the replacement endpoint.
    /// </summary>
    /// <param name="replacement">The endpoint to use instead, e.g. <c>POST /2/account/login</c>.</param>
    public static OpenShockProblem EndpointRetired(string replacement) => new(
        "Endpoint.Retired",
        "This endpoint has been retired",
        HttpStatusCode.Gone,
        $"This endpoint is no longer available. Please use {replacement} instead.");
}
