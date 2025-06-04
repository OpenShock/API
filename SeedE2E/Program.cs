using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    using var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("OpenShock.SeedE2E");

    using var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
    using var transaction = await db.Database.BeginTransactionAsync();

    // Core entities
    await UserSeeder.SeedAsync(db, logger);
    await DeviceSeeder.SeedAsync(db, logger);
    await ShockerSeeder.SeedAsync(db, logger);
    await ControlLogSeeder.SeedAsync(db, logger);

    // API tokens and related reports
    await ApiTokenSeeder.SeedAsync(db, logger);
    await ApiTokenReportSeeder.SeedAsync(db, logger);

    // Device OTA updates
    await DeviceOtaUpdateSeeder.SeedAsync(db, logger);

    // User-related audit tables
    await UserPasswordResetSeeder.SeedAsync(db, logger);
    await UserShareInviteSeeder.SeedAsync(db, logger);
    await UserShareInviteShockerSeeder.SeedAsync(db, logger);
    await UserShareSeeder.SeedAsync(db, logger);
    await ShockerShareCodeSeeder.SeedAsync(db, logger);
    await PublicShareSeeder.SeedAsync(db, logger);
    await PublicShareShockerSeeder.SeedAsync(db, logger);
    await UserEmailChangeSeeder.SeedAsync(db, logger);
    await UserNameChangeSeeder.SeedAsync(db, logger);
    await UserActivationRequestSeeder.SeedAsync(db, logger);
    await UserDeactivationSeeder.SeedAsync(db, logger);

    // Discord webhooks
    await DiscordWebhookSeeder.SeedAsync(db, logger);

    await transaction.CommitAsync();
}

Console.WriteLine("Database seeding complete.");