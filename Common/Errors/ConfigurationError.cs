using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ConfigurationError
{
    public static OpenShockProblem NotFound(string name) => new OpenShockProblem("Configuration.NotFound", $"No configuration named '{name}'.", HttpStatusCode.NotFound);
    public static OpenShockProblem AlreadyExists(string name) => new OpenShockProblem("Configuration.AlreadyExists", $"A configuration named '{name}' already exists.", HttpStatusCode.Conflict);
    public static OpenShockProblem InvalidNameFormat(string name) => new OpenShockProblem("Configuration.InvalidNameFormat", $"Invalid configuration name: '{name}'. Only A–Z and '_' allowed.", HttpStatusCode.BadRequest);
    public static OpenShockProblem InvalidValueFormat(string value) => new OpenShockProblem("Configuration.InvalidValueFormat", $"Value '{value}' is not convertable to config type.", HttpStatusCode.BadRequest);
}