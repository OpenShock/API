using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron;
using OpenShock.Cron.Utils;
using OpenShock.Common.Swagger;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

builder.RegisterCommonOpenShockOptions();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockMemDB(redisConfig);
builder.Services.AddOpenShockDB(databaseConfig);
builder.Services.AddOpenShockServices();

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(databaseConfig.Conn)));
builder.Services.AddHangfireServer();

builder.AddSwaggerExt<Program>();

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