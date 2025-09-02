namespace OpenShock.API.Services.OAuth;

public sealed class CookieOAuthStateStore : IOAuthStateStore
{
    private const string CookiePrefix = "__os_oauth_state_";

    public void Save(HttpContext http, string provider, string state, string? returnTo)
    {
        var val = $"{state}|{returnTo}";
        http.Response.Cookies.Append(CookiePrefix + provider, val, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10), Path = "/"
        });
    }

    public (string State, string? ReturnTo)? ReadAndClear(HttpContext http, string provider)
    {
        var name = CookiePrefix + provider;
        if (!http.Request.Cookies.TryGetValue(name, out var v)) return null;

        http.Response.Cookies.Delete(name, new CookieOptions { Path = "/", HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });

        var i = v.IndexOf('|');
        if (i < 0) return (v, null);
        var s = v[..i];
        var r = v[(i + 1)..];
        return (s, string.IsNullOrWhiteSpace(r) ? null : r);
    }
}