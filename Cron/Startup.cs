using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenTelemetry.Trace;

namespace OpenShock.Cron;

public sealed class Startup
{
    private CronConf _config;

    public Startup(IConfiguration configuration)
    {
#if DEBUG
        var root = (IConfigurationRoot)configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
        _config = configuration.GetChildren()
                      .First(x => x.Key.Equals("openshock", StringComparison.InvariantCultureIgnoreCase))
                      .Get<CronConf>() ??
                  throw new Exception("Couldn't bind config, check config file");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHangfire(hangfire =>
            hangfire.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(_config.Db.Conn)));
        services.AddHangfireServer();
        
        
        services.AddOpenShockServices(_config);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory,
        ILogger<Startup> logger)
    {
        app.UseCommonOpenShockMiddleware();
        
        app.UseHangfireDashboard(options: new DashboardOptions
        {
            AsyncAuthorization = [
                new DashboardAdminAuth()
            ]
        });
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}