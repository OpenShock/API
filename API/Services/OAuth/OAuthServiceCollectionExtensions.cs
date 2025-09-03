using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenShock.API.Services.OAuth;

public static class OAuthServiceCollectionExtensions
{
    public static IOAuthBuilder AddOAuth(this IServiceCollection services)
    {
        // Default state store if none registered
        services.TryAddSingleton<IOAuthStateStore, RedisOAuthStateStore>();

        // Registry built from IEnumerable<IOAuthHandler>
        services.TryAddSingleton<IOAuthHandlerRegistry, OAuthHandlerRegistry>();
        
        return new OAuthBuilder(services);
    }
}