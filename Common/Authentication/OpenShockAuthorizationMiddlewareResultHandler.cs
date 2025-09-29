using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using OpenShock.Common.Errors;

namespace OpenShock.Common.Authentication;

public class OpenShockAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler;

    public OpenShockAuthorizationMiddlewareResultHandler()
    {
        _defaultHandler = new AuthorizationMiddlewareResultHandler();
    }
    
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var failedRequirements = authorizeResult.AuthorizationFailure?.FailedRequirements.Select(x => x.ToString() ?? "error") ?? [];
            return AuthorizationError.PolicyNotMet(failedRequirements).WriteAsJsonAsync(context);
        }
        
        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}