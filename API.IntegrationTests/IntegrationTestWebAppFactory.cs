using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace API.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("openshock")
        .WithUsername("openshock")
        .WithPassword("superSecurePassword")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis/redis-stack-server:latest")
        .Build();


    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    protected override IWebHostBuilder? CreateWebHostBuilder()
    {
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPNETCORE_UNDER_INTEGRATION_TEST", "1" },
            
            { "OPENSHOCK__DB__CONN", _dbContainer.GetConnectionString() },
            { "OPENSHOCK__DB__SKIPMIGRATION", "false" },
            { "OPENSHOCK__DB__DEBUG", "false" },
            
            { "OPENSHOCK__REDIS__CONN", _redisContainer.GetConnectionString() },
            { "OPENSHOCK__REDIS__HOST", "" },
            { "OPENSHOCK__REDIS__USER", "" },
            { "OPENSHOCK__REDIS__PASSWORD", "" },
            { "OPENSHOCK__REDIS__PORT", "6379" },
            
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
    
        return base.CreateWebHostBuilder();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        
        builder.ConfigureTestServices(services =>
        {
            // We can replace services here
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
