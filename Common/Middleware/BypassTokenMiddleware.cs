using System.Security.Cryptography;
using System.Text;
using OneOf.Types;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Configuration;

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

    public async Task InvokeAsync(HttpContext context, IConfigurationService config)
    {
        if (!context.TryGetBypassTokenFromHeader(out var presented))
        {
            await _next(context);
            return;
        }

        var matched = BypassTokenType.None;

        if (await MatchesAsync(config, TurnstileConfigKey, presented)) matched |= BypassTokenType.Turnstile;
        if (await MatchesAsync(config, RateLimitConfigKey, presented)) matched |= BypassTokenType.RateLimit;

        if (matched != BypassTokenType.None) context.SetBypassedTypes(matched);

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
