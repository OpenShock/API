using System.Net;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Errors;

public static class ControlStatsError
{
    public static OpenShockProblem InvalidTimeZone => new("ControlStats.InvalidTimeZone", "Unknown time zone, expected an IANA time zone id like Europe/Berlin", HttpStatusCode.BadRequest);
    public static OpenShockProblem MissingCapability => new("ControlStats.MissingCapability", "An automation token granting ControlStatsRead is required", HttpStatusCode.Forbidden);
    public static OpenShockProblem InvalidRange => new("ControlStats.InvalidRange", "The from date must not be after the to date", HttpStatusCode.BadRequest);
}
