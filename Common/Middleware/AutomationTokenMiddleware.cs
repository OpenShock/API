using System.Security.Claims;
using OpenShock.Common.Extensions;
using OpenShock.Common.Services.AutomationTokens;
using Microsoft.Extensions.Logging;

namespace OpenShock.Common.Middleware;

/// <summary>
/// Resolves the <c>X-OpenShock-Automation-Token</c> header (if present) into a <see cref="ResolvedAutomationToken"/>
/// stored on <see cref="HttpContext.Items"/>. Downstream guards (rate limiter partition selectors,
/// the turnstile service, controllers needing post-auth user linkage) read the cached value
/// synchronously and filter on the type they care about.
///
/// Runs before <c>UseRateLimiter</c> so the rate limiter can honor the bypass for the very same request.
/// </summary>
public sealed class AutomationTokenMiddleware
{
    private readonly RequestDelegate _next;

    public AutomationTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAutomationTokenService automationTokens, ILogger<AutomationTokenMiddleware> logger)
    {
        if (!context.TryGetAutomationTokenFromHeader(out var secret))
        {
            await _next(context);
            return;
        }

        var resolved = await automationTokens.ResolveAsync(secret, context.RequestAborted);
        if (resolved is null)
        {
            // A presented-but-unmatched token is either a stale or rotated secret, or someone probing for one.
            LogUnmatched(logger);
            await _next(context);
            return;
        }

        // Authentication already ran, so a signed-in caller is known here. An automation token never applies
        // to a privileged account, so it is dropped for one, rate limits and Turnstile included.
        // Flows that pick the account from the request body (login, password reset) check the same rule.
        if (context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userIdClaim &&
            Guid.TryParse(userIdClaim, out var userId) &&
            !await automationTokens.IsAllowedForUserAsync(userId, context.RequestAborted))
        {
            logger.LogWarning(
                "Automation token {AutomationTokenName} ({AutomationTokenId}) refused for privileged user {UserId}",
                ForLog(resolved.Name), resolved.Id, userId);
            await _next(context);
            return;
        }

        context.SetResolvedAutomationToken(resolved);
        automationTokens.RecordRequest(resolved.Id);

        // A credential that switches off Turnstile and rate limiting should never be used without
        // leaving a trace. Logged at warning so it stands out in a production log, and the secret
        // itself is never written, only which token was used, what it disabled, and for what.
        logger.LogWarning(
            "Automation token {AutomationTokenName} ({AutomationTokenId}) accepted for {Types} on {Method} {Path} from {RemoteIp}",
            ForLog(resolved.Name), resolved.Id, string.Join(",", resolved.Types), ForLog(context.Request.Method),
            ForLog(context.Request.Path.Value), context.Connection.RemoteIpAddress);

        await _next(context);
    }

    // An unmatched token needs no credential to present, so the warning it produces is volume an
    // unauthenticated caller controls. Keep the audit signal, but collapse it to at most one line
    // per window and report how many attempts that line stands for.
    private static readonly TimeSpan UnmatchedWarningInterval = TimeSpan.FromMinutes(1);
    private static long _unmatchedWindowStartTicks;
    private static long _unmatchedAttempts;

    private static void LogUnmatched(ILogger logger)
    {
        Interlocked.Increment(ref _unmatchedAttempts);

        var now = Environment.TickCount64;
        var windowStart = Interlocked.Read(ref _unmatchedWindowStartTicks);
        if (now - windowStart < UnmatchedWarningInterval.TotalMilliseconds) return;

        // Loser of the race has nothing to report; the winner owns this window's line.
        if (Interlocked.CompareExchange(ref _unmatchedWindowStartTicks, now, windowStart) != windowStart) return;

        var attempts = Interlocked.Exchange(ref _unmatchedAttempts, 0);

        // Deliberately free of request data: the path, method and header are all caller-controlled,
        // and this line exists to say that probing is happening, not to echo the probe back.
        logger.LogWarning(
            "Automation token presented but matched nothing ({Attempts} attempt(s) in the last {Interval})",
            attempts, UnmatchedWarningInterval);
    }

    // Request data would otherwise reach the log verbatim, where a newline in it forges a log line.
    private static string ForLog(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.ReplaceLineEndings(string.Empty);
}
