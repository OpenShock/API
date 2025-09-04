using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.API.OAuth;

public static class OAuthError
{
    // Provider-related
    public static OpenShockProblem UnsupportedProvider => new(
        "OAuth.Provider.Unsupported",
        "The requested OAuth provider is not supported",
        HttpStatusCode.BadRequest);

    public static OpenShockProblem ProviderMismatch => new(
        "OAuth.Provider.Mismatch",
        "The current OAuth flow does not match the requested provider",
        HttpStatusCode.BadRequest);

    // Flow-related
    public static OpenShockProblem UnsupportedFlow => new(
        "OAuth.Flow.Unsupported",
        "This OAuth flow type is not recognized or allowed",
        HttpStatusCode.Forbidden);
    
    public static OpenShockProblem FlowMismatch => new(
        "OAuth.Flow.Mismatch",
        "This OAuth flow differs from the flow the oauth flow started with",
        HttpStatusCode.Forbidden);

    public static OpenShockProblem AnonymousOnlyEndpoint => new(
        "OAuth.Flow.AnonymousOnlyEndpoint",
        "You must be signed out to call this endpoint",
        HttpStatusCode.Unauthorized);

    public static OpenShockProblem FlowStateNotFound => new(
        "OAuth.Flow.NotFound",
        "The OAuth flow was not found, has expired, or is invalid",
        HttpStatusCode.BadRequest);

    public static OpenShockProblem FlowMissingData => new(
        "OAuth.Flow.MissingData",
        "The OAuth provider did not supply the expected identity data",
        HttpStatusCode.BadGateway); // 502 makes sense if external didn't return what we expect

    // Connection-related
    public static OpenShockProblem ConnectionAlreadyExists => new(
        "OAuth.Connection.AlreadyExists",
        "Your account already has an OAuth connection for this provider",
        HttpStatusCode.Conflict);

    public static OpenShockProblem ExternalAlreadyLinked => new(
        "OAuth.Connection.AlreadyLinked",
        "This external account is already linked to another user",
        HttpStatusCode.Conflict);

    public static OpenShockProblem NotAuthenticatedForLink => new(
        "OAuth.Link.NotAuthenticated",
        "You must be signed in to link an external account",
        HttpStatusCode.Unauthorized);

    // Misc / generic
    public static OpenShockProblem InternalError => new(
        "OAuth.InternalError",
        "An unexpected error occurred while processing the OAuth flow",
        HttpStatusCode.InternalServerError);
}