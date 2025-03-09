using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron;
using OpenShock.Cron.Utils;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args, options =>
{
    options.ListenAnyIP(780);
#if DEBUG
    options.ListenAnyIP(7443, options => options.UseHttps("devcert.pfx"));
#endif
});

var config = builder.GetAndRegisterOpenShockConfig<CronConf>();
builder.Services.AddOpenShockServices(config);

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(config.Db.Conn)));
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