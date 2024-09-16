using Hangfire;
using OpenShock.Cron;
using OpenShock.Cron.Jobs;
using OpenShock.Cron.Utils;
using Serilog;

HostBuilder builder = new();
builder.UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables(prefix: "DOTNET_");
        if (args is { Length: > 0 }) config.AddCommandLine(args);
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: false);

        config.AddUserSecrets(typeof(Program).Assembly);
        config.AddEnvironmentVariables();
        if (args is { Length: > 0 }) config.AddCommandLine(args);
    })
    .UseDefaultServiceProvider((context, options) =>
    {
        var isDevelopment = context.HostingEnvironment.IsDevelopment();
        options.ValidateScopes = isDevelopment;
        options.ValidateOnBuild = isDevelopment;
    })
    .UseSerilog((context, _, config) => { config.ReadFrom.Configuration(context.Configuration); })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel();
        webBuilder.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(780);
#if DEBUG
            serverOptions.ListenAnyIP(7443, options => { options.UseHttps("devcert.pfx"); });
#endif
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
        });
        webBuilder.UseStartup<Startup>();
    });
try
{
    var app = builder.Build();

    var jobManagerV2 = app.Services.GetRequiredService<IRecurringJobManagerV2>();
    foreach (var cronJob in CronJobCollector.GetAllCronJobs())
    {
        jobManagerV2.AddOrUpdate(cronJob.Name, cronJob.Job, cronJob.Schedule);
    }

    await app.RunAsync();

}
catch (Exception e)
{
    Console.WriteLine(e);
}