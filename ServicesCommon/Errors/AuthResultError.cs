using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class AuthResultError
{
    public static OpenShockProblem UnknownError => new("Authentication.UnknownError", "An unknown error occurred.", HttpStatusCode.InternalServerError);
    public static OpenShockProblem HeaderMissingOrInvalid => new("Authentication.HeaderMissingOrInvalid", "Missing a required header or it is invalid.", HttpStatusCode.BadRequest);
    
    public static OpenShockProblem SessionInvalid => new("Authentication.SessionInvalid", "The session is invalid", HttpStatusCode.Unauthorized);
    public static OpenShockProblem TokenInvalid => new("Authentication.TokenInvalid", "The token is invalid", HttpStatusCode.Unauthorized);
}