using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron;
using OpenShock.Cron.Utils;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var isDevelopment = builder.Environment.IsDevelopment();
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = isDevelopment;
    options.ValidateOnBuild = isDevelopment;
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(780);
#if DEBUG
    serverOptions.ListenAnyIP(7443, options => options.UseHttps("devcert.pfx"));
#endif
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
});

builder.Services.AddSerilog();

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
    AsyncAuthorization = [
        new DashboardAdminAuth()
    ]
});

app.MapControllers();

var jobManager = app.Services.GetRequiredService<IRecurringJobManagerV2>();
foreach (var cronJob in CronJobCollector.GetAllCronJobs())
{
    jobManager.AddOrUpdate(cronJob.Name, cronJob.Job, cronJob.Schedule);
}

app.Run();