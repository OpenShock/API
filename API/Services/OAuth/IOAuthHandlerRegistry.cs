using OpenShock.API.Models.Response;

namespace OpenShock.API.Services.OAuth;

public interface IOAuthHandlerRegistry
{
    string[] ListProviderKeys();
    bool TryGet(string key, out IOAuthHandler handler);
}