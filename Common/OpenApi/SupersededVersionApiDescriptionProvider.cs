using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Leaves an endpoint out of the OpenAPI document when a newer version of it exists: <c>POST /1/account/login</c> is hidden when
/// <c>POST /2/account/login</c> exists. The old version keeps working, it is just no longer documented.
/// </summary>
/// <remarks>Routes carry their version as a plain leading path segment, e.g. <c>/2/shockers/control</c>.</remarks>
public sealed class SupersededVersionApiDescriptionProvider : IApiDescriptionProvider
{
    // Runs after the providers that fill in the descriptions (their OnProvidersExecuting has completed by now)
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        var latest = new Dictionary<(string?, string), int>();
        foreach (var description in context.Results)
        {
            if (!TryGetVersion(description, out var key, out var version)) continue;
            if (!latest.TryGetValue(key, out var current) || version > current) latest[key] = version;
        }

        for (var i = context.Results.Count - 1; i >= 0; i--)
        {
            if (TryGetVersion(context.Results[i], out var key, out var version) && version < latest[key]) context.Results.RemoveAt(i);
        }
    }

    private static bool TryGetVersion(ApiDescription description, out (string? HttpMethod, string Path) key, out int version)
    {
        var path = description.RelativePath ?? string.Empty;
        var slash = path.IndexOf('/');

        if (int.TryParse(slash < 0 ? path : path[..slash], out version))
        {
            key = (description.HttpMethod?.ToUpperInvariant(), slash < 0 ? string.Empty : path[(slash + 1)..].ToLowerInvariant());
            return true;
        }

        key = default;
        return false;
    }
}
