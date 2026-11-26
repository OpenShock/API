using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;

namespace OpenShock.Common.Hubs;

/// <summary>
/// Re-checks, before every hub method call, that the session or API token a connection authenticated with is
/// still valid, and aborts the connection when it is not.
/// </summary>
/// <remarks>
/// SignalR authenticates a connection once, when it opens, and then keeps it for as long as the client likes.
/// Without this, logging a session out or revoking, expiring or deactivating an API token would only stop new
/// connections: an open one could keep calling, and whatever calls it already had queued would still run.
/// Aborting also discards those queued calls.
/// <para>
/// For an API token the row read here is handed to the hub method through
/// <see cref="HubCallerContextExtensions.GetCurrentApiToken"/>, so permissions, pause state and control limits
/// are the current ones rather than the ones from connect time.
/// </para>
/// </remarks>
public sealed class RevalidateAuthenticationHubFilter : IHubFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (!await IsStillAuthenticated(invocationContext))
        {
            invocationContext.Context.Abort();
            throw new HubException("Authentication is no longer valid");
        }

        return await next(invocationContext);
    }

    private static async Task<bool> IsStillAuthenticated(HubInvocationContext invocationContext)
    {
        var context = invocationContext.Context;
        var user = context.User;

        // Anonymous connection (a public share guest): nothing was granted that could have been revoked.
        if (user is null) return true;

        if (user.HasOpenShockApiTokenIdentity())
        {
            if (!user.TryGetClaimValueAsGuid(OpenShockAuthClaims.ApiTokenId, out var tokenId)) return false;

            var db = invocationContext.ServiceProvider.GetRequiredService<OpenShockContext>();
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
            var sessionService = invocationContext.ServiceProvider.GetRequiredService<ISessionService>();
            var session = await sessionService.GetSessionByTokenAsync(sessionToken);
            if (session is null || session.UserId != identity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier)) return false;

            // That deletion happens in Redis after the database commit and can fail, leaving the session
            // behind. The account itself is the authority, as it is when a session authenticates a request.
            var db = invocationContext.ServiceProvider.GetRequiredService<OpenShockContext>();
            return await db.Users.AnyAsync(u => u.Id == session.UserId && u.ActivatedAt != null && u.UserDeactivation == null, context.ConnectionAborted);
        }

        return true;
    }
}

public static class HubCallerContextExtensions
{
    internal static readonly object CurrentApiTokenKey = new();

    /// <summary>
    /// The API token this connection authenticated with, as it is in the database at the time of the current
    /// call. Null for a session or anonymous connection, and outside of a hub method call.
    /// </summary>
    public static ApiToken? GetCurrentApiToken(this HubCallerContext context)
        => context.Items.TryGetValue(CurrentApiTokenKey, out var token) ? token as ApiToken : null;
}
