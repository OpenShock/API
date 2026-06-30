using Hangfire;
using Hangfire.PostgreSql;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Cron;
using OpenShock.Cron.Services.Email;
using OpenShock.Cron.Utils;
using OpenShock.Common.Swagger;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();
// The outbox dispatcher builds activation/reset links, so it needs the frontend base URL.
builder.RegisterFrontendOptions();

builder.Services.AddOpenShockMemDB(redisOptions);
builder.Services.AddOpenShockDB(databaseOptions);
builder.Services.AddOpenShockServices();

// Hangfire workers fetch enqueued jobs by polling the queue table. The default interval (15s) is
// left as-is in production - email delivery within that window is fine and it adds no DB load. Only
// the integration test overrides it (via OpenShock:Hangfire:QueuePollInterval) to run fast.
var hangfireStorageOptions = new PostgreSqlStorageOptions();
if (builder.Configuration.GetValue<TimeSpan?>("OpenShock:Hangfire:QueuePollInterval") is { } queuePollInterval)
    hangfireStorageOptions.QueuePollInterval = queuePollInterval;

builder.Services.AddHangfire(hangfire =>
    hangfire.UsePostgreSqlStorage(
        c => c.UseNpgsqlConnection(databaseOptions.Conn),
        hangfireStorageOptions));
builder.Services.AddHangfireServer();

// Registers the email providers, the outbox dispatcher, the per-email Hangfire send job, and the
// outbox consumer that hands pending rows off to Hangfire. The API host only writes outbox rows;
// all sending happens here.
await builder.AddEmailService();

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

// Expose Program for integration tests (so the test host can boot the Cron pipeline in-process).
public partial class Program;