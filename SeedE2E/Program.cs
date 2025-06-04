using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
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

builder.Services.AddOpenShockDB(databaseConfig);
builder.Services.AddOpenShockServices();

var app = builder.Build();

await app.ApplyPendingOpenShockMigrations(databaseConfig);

// --- SEED ALL THE DATABASE TABLES ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

    // Core entities
    await UserSeeder.SeedAsync(db);
    await DeviceSeeder.SeedAsync(db);
    await ShockerSeeder.SeedAsync(db);
    await ControlLogSeeder.SeedAsync(db);

    // API tokens and related reports
    await ApiTokenSeeder.SeedAsync(db);
    await ApiTokenReportSeeder.SeedAsync(db);

    // Device OTA updates
    await DeviceOtaUpdateSeeder.SeedAsync(db);

    // User-related audit tables
    await UserPasswordResetSeeder.SeedAsync(db);
    await UserShareInviteSeeder.SeedAsync(db);
    await UserShareInviteShockerSeeder.SeedAsync(db);
    await UserShareSeeder.SeedAsync(db);
    await ShockerShareCodeSeeder.SeedAsync(db);
    await PublicShareSeeder.SeedAsync(db);
    await PublicShareShockerSeeder.SeedAsync(db);
    await UserEmailChangeSeeder.SeedAsync(db);
    await UserNameChangeSeeder.SeedAsync(db);
    await UserActivationRequestSeeder.SeedAsync(db);
    await UserDeactivationSeeder.SeedAsync(db);

    // Discord webhooks
    await DiscordWebhookSeeder.SeedAsync(db);
}

Console.WriteLine("Database seeding complete.");