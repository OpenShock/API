using System.Security.Cryptography;
using System.Text;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenShock.Common.Middleware;

/// <summary>
/// Resolves the <c>X-OpenShock-Bypass-Token</c> header by comparing it to admin-set configuration
/// properties (<c>TURNSTILE_BYPASS_TOKEN</c>, <c>RATE_LIMIT_BYPASS_TOKEN</c>). The matched bypass
/// flags are stored on <see cref="HttpContext.Items"/> so downstream guards (rate limiter selectors,
/// turnstile service) can read them synchronously.
///
/// Runs before <c>UseRateLimiter</c>.
/// </summary>
public sealed class BypassTokenMiddleware
{
    public const string TurnstileConfigKey = "TURNSTILE_BYPASS_TOKEN";
    public const string RateLimitConfigKey = "RATE_LIMIT_BYPASS_TOKEN";

    private readonly RequestDelegate _next;

    public BypassTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfigurationService config, ILogger<BypassTokenMiddleware> logger)
    {
        if (!context.TryGetBypassTokenFromHeader(out var presented))
        {
            await _next(context);
            return;
        }

        var matched = BypassTokenType.None;

        if (await MatchesAsync(config, TurnstileConfigKey, presented)) matched |= BypassTokenType.Turnstile;
        if (await MatchesAsync(config, RateLimitConfigKey, presented)) matched |= BypassTokenType.RateLimit;

        if (matched != BypassTokenType.None)
        {
            context.SetBypassedTypes(matched);

            // A credential that switches off Turnstile and rate limiting should never be used without
            // leaving a trace. Logged at warning so it stands out in a production log, and the token
            // itself is never written - only which protections it disabled, and for what.
            logger.LogWarning(
                "Bypass token accepted for {Matched} on {Method} {Path} from {RemoteIp}",
                matched, ForLog(context.Request.Method), ForLog(context.Request.Path.Value), context.Connection.RemoteIpAddress);
        }
        else
        {
            // A presented-but-unmatched token is either a stale secret or someone probing for one.
            LogUnmatched(logger);
        }

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
            "Bypass token presented but matched nothing ({Attempts} attempt(s) in the last {Interval})",
            attempts, UnmatchedWarningInterval);
    }

    // Request data would otherwise reach the log verbatim, where a newline in it forges a log line.
    private static string ForLog(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.ReplaceLineEndings(string.Empty);

    private static async Task<bool> MatchesAsync(IConfigurationService config, string key, string presented)
    {
        var result = await config.TryGetStringAsync(key);
        var configured = result.Outcome == ConfigGetOutcome.Value ? result.Value : null;
        return !string.IsNullOrEmpty(configured)
               && CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(configured),
                   Encoding.UTF8.GetBytes(presented));
    }
}
