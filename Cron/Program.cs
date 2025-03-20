using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron.Utils;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args, options =>
{
    options.ListenAnyIP(780);
#if DEBUG
    options.ListenAnyIP(7443, options => options.UseHttps("devcert.pfx"));
#endif
});

builder.RegisterCommonOpenShockOptions();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockServices(databaseConfig, redisConfig);

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(databaseConfig.Conn)));
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseCommonOpenShockMiddleware();

app.UseHangfireDashboard(options: new DashboardOptions
{
#if !DEBUG
    AsyncAuthorization =
    [
        new DashboardAdminAuth()
    ]
#endif
});

app.MapControllers();

var jobManager = app.Services.GetRequiredService<IRecurringJobManagerV2>();
foreach (var cronJob in CronJobCollector.GetAllCronJobs())
{
    jobManager.AddOrUpdate(cronJob.Name, cronJob.Job, cronJob.Schedule);
}

app.Run();