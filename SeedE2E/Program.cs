using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using OpenShock.SeedE2E.Seeders;

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

// --- SEED ALL THE DATABASE TABLES ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

    await UserSeeder.SeedAsync(db);
    await DeviceSeeder.SeedAsync(db);
    await ShockerSeeder.SeedAsync(db);
    await ControlLogSeeder.SeedAsync(db);

    Console.WriteLine("Database seeding complete.");
}

await app.RunAsync();