using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenShock.Common.OpenShockDb;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace API.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("openshock")
        .WithUsername("openshock")
        .WithPassword("superSecurePassword")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        _redisContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OpenShockContext>>();
            services.AddDbContext<OpenShockContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString()));

            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
