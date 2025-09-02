namespace OpenShock.API.Services.OAuth;

public interface IOAuthHandlerRegistry
{
    string[] ListProviders();
    bool TryGet(string key, out IOAuthHandler handler);
}