using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Hubs;

/// <summary>
/// The authenticated hub connections open on this instance, by what they authenticated with. Lets a
/// revocation that arrives over Redis close the affected connections right away, instead of each connection
/// finding out the next time it asks the database (see <see cref="RevalidateAuthenticationHubFilter"/>).
/// </summary>
/// <remarks>
/// Local to the process: every instance tracks its own connections and reacts to the same Redis message.
/// Lookups scan all connections, which is fine for how rarely sessions and tokens are revoked.
/// </remarks>
public sealed class HubConnectionTracker
{
    private readonly ConcurrentDictionary<string, TrackedHubConnection> _connections = new();

    public TrackedHubConnection? Get(string connectionId)
        => _connections.GetValueOrDefault(connectionId);

    /// <summary>Starts tracking a connection. Anonymous connections hold nothing that can be revoked and are skipped.</summary>
    /// <returns>The tracked connection, or null when it was skipped.</returns>
    public TrackedHubConnection? Track(HubCallerContext context)
    {
        var user = context.User;
        if (user is null) return null;

        Guid? userId = Guid.TryParse(context.UserIdentifier, out var parsedUserId) ? parsedUserId : null;

        if (user.HasOpenShockApiTokenIdentity())
        {
            if (!user.TryGetClaimValueAsGuid(OpenShockAuthClaims.ApiTokenId, out var tokenId)) return null;

            return _connections[context.ConnectionId] = new TrackedHubConnection(context, userId, sessionId: null, tokenId);
        }

        if (user.TryGetOpenShockUserIdentity() is not null
            && context.GetHttpContext() is { } httpContext
            && httpContext.TryGetUserSessionToken(out var sessionToken))
        {
            return _connections[context.ConnectionId] = new TrackedHubConnection(context, userId, GetSessionId(sessionToken), apiTokenId: null);
        }

        return null;
    }

    public void Untrack(string connectionId) => _connections.TryRemove(connectionId, out _);

    /// <summary>Closes the connections that authenticated with this session.</summary>
    /// <param name="sessionId">The id the session is stored under, see <see cref="GetSessionId"/>.</param>
    /// <returns>How many connections were closed.</returns>
    public int AbortSession(string sessionId) => Abort(c => c.SessionId == sessionId);

    /// <summary>Closes every connection of this user, whatever it authenticated with.</summary>
    /// <returns>How many connections were closed.</returns>
    public int AbortUser(Guid userId) => Abort(c => c.UserId == userId);

    /// <summary>Closes the connections that authenticated with this API token.</summary>
    /// <returns>How many connections were closed.</returns>
    public int AbortApiToken(Guid tokenId) => Abort(c => c.ApiTokenId == tokenId);

    public bool HasApiTokenConnections(Guid tokenId)
    {
        foreach (var connection in _connections.Values)
        {
            if (connection.ApiTokenId == tokenId) return true;
        }

        return false;
    }

    /// <summary>
    /// Makes the connections using this API token read it from the database again on their next call, so a
    /// change to its permissions, pause state or limits applies from then on.
    /// </summary>
    public void InvalidateApiToken(Guid tokenId)
    {
        foreach (var connection in _connections.Values)
        {
            if (connection.ApiTokenId == tokenId) connection.Invalidate();
        }
    }

    /// <summary>The id a session is stored under in Redis: the hash of its token (old tokens are stored as they are).</summary>
    public static string GetSessionId(string sessionToken)
        => sessionToken.Length == AuthConstants.GeneratedTokenLength ? HashingUtils.HashToken(sessionToken) : sessionToken;

    private int Abort(Func<TrackedHubConnection, bool> predicate)
    {
        var aborted = 0;
        foreach (var (connectionId, connection) in _connections)
        {
            if (!predicate(connection)) continue;

            // Dropped here as well as on disconnect, so a message that arrives twice finds nothing.
            _connections.TryRemove(connectionId, out _);
            connection.Context.Abort();
            aborted++;
        }

        return aborted;
    }
}

public sealed class TrackedHubConnection
{
    // 0 means "not validated": Stopwatch timestamps are never 0 in practice, and a false "not validated"
    // only costs one extra check.
    private long _lastValidated;

    public TrackedHubConnection(HubCallerContext context, Guid? userId, string? sessionId, Guid? apiTokenId)
    {
        Context = context;
        UserId = userId;
        SessionId = sessionId;
        ApiTokenId = apiTokenId;
    }

    public HubCallerContext Context { get; }
    public Guid? UserId { get; }
    public string? SessionId { get; }
    public Guid? ApiTokenId { get; }

    public bool WasValidatedWithin(TimeSpan interval)
    {
        var last = Volatile.Read(ref _lastValidated);
        return last != 0 && Stopwatch.GetElapsedTime(last) < interval;
    }

    public void MarkValidated() => Volatile.Write(ref _lastValidated, Stopwatch.GetTimestamp());

    public void Invalidate() => Volatile.Write(ref _lastValidated, 0);
}
