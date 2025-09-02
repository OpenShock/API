namespace OpenShock.API.Services.OAuth;

public interface IOAuthBuilder
{
    IOAuthBuilder AddHandler<THandler, TOptions>(string key, IConfiguration configuration)
        where THandler : class, IOAuthHandler
        where TOptions : class;
}