using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ExpressionError
{
    public static OpenShockProblem QueryStringInvalidError(string details) => new OpenShockProblem("ExpressionError", "Query string is invalid", HttpStatusCode.BadRequest, details);
    public static OpenShockProblem ExpressionExceptionError(string details) => new OpenShockProblem("ExpressionError", "An error occured while processing the expression", HttpStatusCode.BadRequest, details);
}