using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenShock.API.Services.OAuth;

public interface IOAuthBuilder
{
    IOAuthBuilder AddHandler<THandler, TOptions>(IConfiguration configuration)
        where THandler : class, IOAuthHandler
        where TOptions : class;
}

internal sealed class OAuthBuilder : IOAuthBuilder
{
    private readonly IServiceCollection _services;
    internal OAuthBuilder(IServiceCollection services) => _services = services;

    public IOAuthBuilder AddHandler<THandler, TOptions>(IConfiguration configuration)
        where THandler : class, IOAuthHandler
        where TOptions : class
    {
        _services.Configure<TOptions>(configuration);

        // Typed HttpClient per handler (unique type = unique client)
        _services.AddHttpClient<THandler>();

        // Register handler as IOAuthHandler
        _services.AddSingleton<IOAuthHandler, THandler>();

        return this;
    }
}

public static class ServiceCollectionHelpers
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