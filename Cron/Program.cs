using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenAPI;
using OpenShock.Cron;
using OpenShock.Cron.Utils;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();

builder.Services.AddOpenShockMemDB(redisOptions);
builder.Services.AddOpenShockDB(databaseOptions);
builder.Services.AddOpenShockServices();

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(databaseOptions.Conn)));
builder.Services.AddHangfireServer();

builder.AddOpenApiExt<Program>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

var hangfireOptions = new DashboardOptions();
if (app.Environment.IsProduction() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    hangfireOptions.Authorization = [ ];
    hangfireOptions.AsyncAuthorization = [ new DashboardAdminAuth() ];
}

app.UseHangfireDashboard(options: hangfireOptions);

var jobManager = app.Services.GetRequiredService<IRecurringJobManagerV2>();
foreach (var cronJob in CronJobCollector.GetAllCronJobs())
{
    jobManager.AddOrUpdate(cronJob.Name, cronJob.Job, cronJob.Schedule);
}

await app.RunAsync();