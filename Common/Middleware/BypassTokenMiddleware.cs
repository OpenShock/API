using System.Security.Cryptography;
using System.Text;
using OneOf.Types;
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
                matched, context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
        }
        else
        {
            // A presented-but-unmatched token is either a stale secret or someone probing for one.
            logger.LogWarning(
                "Bypass token presented but matched nothing on {Method} {Path} from {RemoteIp}",
                context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
        }

        await _next(context);
    }

    private static async Task<bool> MatchesAsync(IConfigurationService config, string key, string presented)
    {
        var result = await config.TryGetStringAsync(key);
        return result.TryPickT0(out var configured, out _)
               && !string.IsNullOrEmpty(configured)
               && CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(configured),
                   Encoding.UTF8.GetBytes(presented));
    }
}
