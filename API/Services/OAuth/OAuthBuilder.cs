namespace OpenShock.API.Services.OAuth;

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