using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Options;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine(builder.Configuration.GetDebugView());
}

builder.RegisterCommonOpenShockOptions();

builder.Services.Configure<FrontendOptions>(builder.Configuration.GetRequiredSection(FrontendOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<FrontendOptions>, FrontendOptionsValidator>();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockMemDB(redisConfig);
builder.Services.AddOpenShockDB(databaseConfig);
builder.Services.AddOpenShockServices();

var app = builder.Build();

await app.ApplyPendingOpenShockMigrations(databaseConfig);

await app.RunAsync();