namespace OpenShock.API.Services.OAuth;

public sealed class OAuthHandlerRegistry : IOAuthHandlerRegistry
{
    private readonly Dictionary<string, IOAuthHandler> _handlers;

    public OAuthHandlerRegistry(IEnumerable<IOAuthHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Key, h => h, StringComparer.OrdinalIgnoreCase);
    }
    
    public string[] ListProviders() => _handlers.Keys.ToArray();

    public bool TryGet(string key, out IOAuthHandler handler)
        => _handlers.TryGetValue(key, out handler!);
}