using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class OAuthError
{
    public static OpenShockProblem ProviderNotSupported => new OpenShockProblem(
        "OAuth.Provider.NotSupported", "This OAuth provider is not supported", HttpStatusCode.Forbidden);
    public static OpenShockProblem ProviderMismatch => new OpenShockProblem(
        "OAuth.Provider.Mismatch", "????????????????", HttpStatusCode.BadRequest);
    
    public static OpenShockProblem FlowNotSupported => new OpenShockProblem(
        "OAuth.Flow.NotSupported", "This OAuth flow is not supported", HttpStatusCode.Forbidden);
    public static OpenShockProblem FlowNotFound => new OpenShockProblem(
        "OAuth.Flow.NotFound", "This OAuth flow is expired or invalid", HttpStatusCode.BadRequest);
    
    public static OpenShockProblem FlowMissingData => new OpenShockProblem(
        "OAuth.Flow.MissingData", "The OAuth provider supplied less data that expected", HttpStatusCode.InternalServerError);

    public static OpenShockProblem AlreadyExists  => new OpenShockProblem(
        "OAuth.Connection.AlreadyExists", "There is already an OAuth connection of this type in your account", HttpStatusCode.Conflict);
    
    public static OpenShockProblem LinkedToAnotherAccount => new OpenShockProblem(
        "OAuth.Connection.LinkedToStranger", "This external account is already linked to another user", HttpStatusCode.Conflict);
    
    public static OpenShockProblem InternalError => new OpenShockProblem(
        "OAuth.InternalError", "Encountered an unexpected error while processing your OAuth flow", HttpStatusCode.InternalServerError);
}