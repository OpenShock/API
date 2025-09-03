using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;

namespace OpenShock.API.Services.OAuth;

public sealed class RedisOAuthStateStore : IOAuthStateStore
{
    private const string CookiePrefix = "__os_oauth_state_";

    private readonly IRedisCollection<OAuthStateEntry> _states;

    public RedisOAuthStateStore(IRedisConnectionProvider redis)
    {
        // No indexing needed for lookups by Id, but JSON storage is convenient
        _states = redis.RedisCollection<OAuthStateEntry>(false);
    }

    public async Task SaveAsync(HttpContext http, OAuthStateEnvelope env, TimeSpan ttl)
    {
        // Persist server-side (source of truth)
        var entry = Map(env);
        await _states.InsertAsync(entry, ttl);

        // Double-submit cookie with the same nonce (no signing/encryption needed)
        http.Response.Cookies.Append(CookiePrefix + env.Provider, env.State, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(ttl),
            Path = "/"
        });
    }

    public async Task<OAuthStateEnvelope?> ReadAndClearAsync(HttpContext http, string provider, string state)
    {
        // Optional: verify cookie matches the returned state (defense-in-depth)
        var cookieName = CookiePrefix + provider;
        if (!http.Request.Cookies.TryGetValue(cookieName, out var cookieState) ||
            !string.Equals(cookieState, state, StringComparison.Ordinal))
        {
            return null;
        }

        // Remove cookie regardless of Redis hit (one-time use)
        http.Response.Cookies.Delete(cookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        // Load & delete atomically-ish (best-effort; Redis OM lacks multi here, but TTL + delete is fine)
        var id = OAuthStateEntry.MakeId(provider, state);
        var entry = await _states.FindByIdAsync(id);
        if (entry is null)
            return null;

        await _states.DeleteAsync(entry);
        return Map(entry);
    }

    private static OAuthStateEntry Map(OAuthStateEnvelope e) => new()
    {
        Id = OAuthStateEntry.MakeId(e.Provider, e.State),
        Provider = e.Provider,
        State = e.State,
        Flow = e.Flow,
        ReturnTo = e.ReturnTo,
        UserId = e.UserId,
        CreatedAt = e.CreatedAt.UtcDateTime
    };

    private static OAuthStateEnvelope Map(OAuthStateEntry e) => new(
        Provider: e.Provider,
        State: e.State,
        Flow: e.Flow,
        ReturnTo: e.ReturnTo,
        UserId: e.UserId,
        CreatedAt: DateTime.SpecifyKind(e.CreatedAt, DateTimeKind.Utc));

    // Redis JSON document
    [Document(StorageType = StorageType.Json, Prefixes = new[] { "oauth:state" })]
    public sealed class OAuthStateEntry
    {
        [RedisIdField] public required string Id { get; set; }               // oauth:state:{provider}:{state}
        public required string Provider { get; set; }
        public required string State { get; set; }
        public required OAuthFlow Flow { get; set; }
        public required string ReturnTo { get; set; }
        public required Guid? UserId { get; set; }
        public required DateTime CreatedAt { get; set; }

        public static string MakeId(string provider, string state) => $"{provider}:{state}";
    }
}