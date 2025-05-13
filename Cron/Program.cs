using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron;
using OpenShock.Cron.Utils;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

builder.RegisterCommonOpenShockOptions();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockServices(databaseConfig, redisConfig);

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(databaseConfig.Conn)));
builder.Services.AddHangfireServer();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

var hangfireOptions = new DashboardOptions();
if (app.Environment.IsProduction())
{
    hangfireOptions.AsyncAuthorization = [ new DashboardAdminAuth() ];
}

app.UseHangfireDashboard(options: hangfireOptions);

var jobManager = app.Services.GetRequiredService<IRecurringJobManagerV2>();
foreach (var cronJob in CronJobCollector.GetAllCronJobs())
{
    jobManager.AddOrUpdate(cronJob.Name, cronJob.Job, cronJob.Schedule);
}

app.Run();