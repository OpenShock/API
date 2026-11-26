using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;

namespace OpenShock.Common.Hubs;

/// <summary>
/// Keeps an open hub connection from outliving the session or API token it authenticated with.
/// </summary>
/// <remarks>
/// SignalR authenticates a connection once, when it opens, and then keeps it for as long as the client likes.
/// Without this, logging a session out or revoking, expiring or deactivating an API token would only stop new
/// connections: an open one could keep calling, and whatever calls it already had queued would still run.
/// Aborting also discards those queued calls.
/// <para>
/// Two things end a connection. The fast one is a Redis message: revocations are published, and the instance
/// holding the connection closes it right away (the filter registers every connection with
/// <see cref="HubConnectionTracker"/> for that). The slow one is the check in this filter, which asks the
/// database before a hub method call. It exists for what the messages cannot cover, a message that was lost
/// or a token that simply reached its expiry, so it only runs once per
/// <see cref="HubRevalidationOptions.Interval"/> per connection, and never for an idle one.
/// </para>
/// <para>
/// For an API token the row read here is handed to the hub method through
/// <see cref="HubCallerContextExtensions.GetCurrentApiToken"/>. A change to the token invalidates the
/// connection's last check, so its permissions, pause state and control limits are read again on the next call.
/// </para>
/// </remarks>
public sealed class RevalidateAuthenticationHubFilter : IHubFilter
{
    private readonly HubConnectionTracker _tracker;
    private readonly IOptions<HubRevalidationOptions> _options;

    public RevalidateAuthenticationHubFilter(HubConnectionTracker tracker, IOptions<HubRevalidationOptions> options)
    {
        _tracker = tracker;
        _options = options;
    }

    /// <inheritdoc />
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var tracked = _tracker.Track(context.Context);

        // The connection was authenticated before it was tracked. A revocation announced in between found
        // nothing to close, so look once now that announcements can reach it.
        if (tracked is not null)
        {
            if (!await IsStillAuthenticated(context.Context, context.ServiceProvider))
            {
                _tracker.Untrack(context.Context.ConnectionId);
                context.Context.Abort();
                return;
            }

            tracked.MarkValidated();
        }

        await next(context);
    }

    /// <inheritdoc />
    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        _tracker.Untrack(context.Context.ConnectionId);
        return next(context, exception);
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var context = invocationContext.Context;

        // Untracked means anonymous (nothing to check), or a connection a revocation already closed whose
        // queued calls are still draining: the check below turns those away.
        var tracked = _tracker.Get(context.ConnectionId);

        if (tracked is null || !tracked.WasValidatedWithin(_options.Value.Interval))
        {
            if (!await IsStillAuthenticated(context, invocationContext.ServiceProvider))
            {
                context.Abort();
                throw new HubException("Authentication is no longer valid");
            }

            tracked?.MarkValidated();
        }

        return await next(invocationContext);
    }

    private static async Task<bool> IsStillAuthenticated(HubCallerContext context, IServiceProvider services)
    {
        var user = context.User;

        // Anonymous connection (a public share guest): nothing was granted that could have been revoked.
        if (user is null) return true;

        if (user.HasOpenShockApiTokenIdentity())
        {
            if (!user.TryGetClaimValueAsGuid(OpenShockAuthClaims.ApiTokenId, out var tokenId)) return false;

            var db = services.GetRequiredService<OpenShockContext>();
            var token = await db.ApiTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == tokenId &&
                    (x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow) &&
                    x.User.UserDeactivation == null, context.ConnectionAborted);
            if (token is null) return false;

            context.Items[HubCallerContextExtensions.CurrentApiTokenKey] = token;
            return true;
        }

        if (user.TryGetOpenShockUserIdentity() is { } identity)
        {
            if (context.GetHttpContext() is not { } httpContext || !httpContext.TryGetUserSessionToken(out var sessionToken)) return false;

            // Sessions are deleted on logout, on deactivation and on a password reset.
            var sessionService = services.GetRequiredService<ISessionService>();
            var session = await sessionService.GetSessionByTokenAsync(sessionToken);
            if (session is null || session.UserId != identity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier)) return false;

            // That deletion happens in Redis after the database commit and can fail, leaving the session
            // behind. The account itself is the authority, as it is when a session authenticates a request.
            var db = services.GetRequiredService<OpenShockContext>();
            return await db.Users.AnyAsync(u => u.Id == session.UserId && u.ActivatedAt != null && u.UserDeactivation == null, context.ConnectionAborted);
        }

        return true;
    }
}

public sealed class HubRevalidationOptions
{
    /// <summary>
    /// How long a connection's last successful database check stays good. Revocations normally close a
    /// connection at once through Redis; this bounds how long one can go on when that message was lost, or
    /// when an API token expired on its own.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
}

public static class HubCallerContextExtensions
{
    internal static readonly object CurrentApiTokenKey = new();

    /// <summary>
    /// The API token this connection authenticated with, as it was in the database when the connection was
    /// last checked. Null for a session or anonymous connection.
    /// </summary>
    public static ApiToken? GetCurrentApiToken(this HubCallerContext context)
        => context.Items.TryGetValue(CurrentApiTokenKey, out var token) ? token as ApiToken : null;
}
