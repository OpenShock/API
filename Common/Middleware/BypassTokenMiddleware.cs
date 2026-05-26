using OpenShock.Common.Extensions;
using OpenShock.Common.Services.Bypass;

namespace OpenShock.Common.Middleware;

/// <summary>
/// Resolves the <c>X-OpenShock-Bypass-Token</c> header (if present) into a <see cref="ResolvedBypassToken"/>
/// stored on <see cref="HttpContext.Items"/>. Downstream guards (rate limiter partition selectors,
/// the turnstile service, controllers needing post-auth user linkage) read the cached value
/// synchronously and filter on the type they care about.
///
/// Runs before <c>UseRateLimiter</c> so the rate limiter can honor the bypass for the very same request.
/// </summary>
public sealed class BypassTokenMiddleware
{
    private readonly RequestDelegate _next;

    public BypassTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IBypassTokenService bypassTokens)
    {
        if (context.TryGetBypassTokenFromHeader(out var secret))
        {
            var resolved = await bypassTokens.ResolveAsync(secret, context.RequestAborted);
            context.SetResolvedBypassToken(resolved);
        }

        await _next(context);
    }
}
