using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using OpenShock.API.IntegrationTests.Docker;
using OpenShock.API.IntegrationTests.HttpMessageHandlers;
using TUnit.Core.Interfaces;

namespace OpenShock.API.IntegrationTests;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryDatabase PostgreSql { get; init; }
    
    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // TODO: Find a way to do the following instead of the current implementation
        /*
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.Sources.Clear();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ...
            });
        });
        */
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPNETCORE_UNDER_INTEGRATION_TEST", "1" },
            
            { "OPENSHOCK__DB__CONN", PostgreSql.Container.GetConnectionString() },
            { "OPENSHOCK__DB__SKIPMIGRATION", "false" },
            { "OPENSHOCK__DB__DEBUG", "false" },
            
            { "OPENSHOCK__REDIS__CONN", Redis.Container.GetConnectionString() },
            
            { "OPENSHOCK__FRONTEND__BASEURL", "https://openshock.app" },
            { "OPENSHOCK__FRONTEND__SHORTURL", "https://openshock.app" },
            { "OPENSHOCK__FRONTEND__COOKIEDOMAIN", "openshock.app" },
            
            { "OPENSHOCK__MAIL__TYPE", "MAILJET" },
            { "OPENSHOCK__MAIL__SENDER__EMAIL", "system@openshock.org" },
            { "OPENSHOCK__MAIL__SENDER__NAME", "OpenShock" },
            { "OPENSHOCK__MAIL__MAILJET__KEY", "mailjet-key" },
            { "OPENSHOCK__MAIL__MAILJET__SECRET", "mailjet-secret" },
            { "OPENSHOCK__MAIL__MAILJET__TEMPLATE__PASSWORDRESET", "12345678" },
            { "OPENSHOCK__MAIL__MAILJET__TEMPLATE__PASSWORDRESETCOMPLETE", "87654321" },
            { "OPENSHOCK__MAIL__MAILJET__TEMPLATE__VERIFYEMAIL", "11223344" },
            { "OPENSHOCK__MAIL__MAILJET__TEMPLATE__VERIFYEMAILCOMPLETE", "44332211" },
            
            { "OPENSHOCK__TURNSTILE__ENABLED", "true" },
            { "OPENSHOCK__TURNSTILE__SECRETKEY", "turnstile-secret-key" },
            { "OPENSHOCK__TURNSTILE__SITEKEY", "turnstile-site-key" },
            
            { "OPENSHOCK__LCG__FQDN", "de1-gateway.my-openshock-instance.net" },
            { "OPENSHOCK__LCG__COUNTRYCODE", "DE" }
        };
    
        foreach (var envVar in environmentVariables)
        {
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
        }
    
        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<HttpMessageHandlerBuilder, InterceptedHttpMessageHandlerBuilder>();
        });
    }
}
