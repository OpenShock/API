using System.Net;
using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Email.Mailjet;
using OpenShock.API.Services.Email.Smtp;
using OpenShock.Common;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.DataAnnotations;
using OpenShock.ServicesCommon.ExceptionHandle;
using OpenShock.ServicesCommon.Geo;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.Device;
using OpenShock.ServicesCommon.Services.Ota;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using OpenShock.ServicesCommon.Services.Turnstile;
using OpenShock.ServicesCommon.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Semver;
using Serilog;
using StackExchange.Redis;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace OpenShock.API;

public class Startup
{
    private readonly ForwardedHeadersOptions _forwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        ForwardedForHeaderName = "CF-Connecting-IP"
    };

    public Startup(IConfiguration configuration)
    {
        APIGlobals.ApiConfig = configuration.GetChildren()
                                   .First(x => x.Key.Equals("openshock", StringComparison.InvariantCultureIgnoreCase))
                                   .Get<ApiConfig>() ??
                               throw new Exception("Couldn't bind config, check config file");

        var startupLogger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        MiniValidation.MiniValidator.TryValidate(APIGlobals.ApiConfig, true, true, out var errors);
        if (errors.Count > 0)
        {
            var sb = new StringBuilder();

            foreach (var error in errors)
            {
                sb.AppendLine($"Error on field [{error.Key}] reason: {string.Join(", ", error.Value)}");
            }

            startupLogger.Error(
                "Error validating config, please fix your configuration / environment variables\nFound the following errors:\n{Errors}",
                sb.ToString());
            Environment.Exit(-10);
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // ----------------- DATABASE -----------------

        // How do I do this now with EFCore?!
#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ControlType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PermissionType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ShockerModelType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<RankType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<OtaUpdateStatus>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PasswordEncryptionType>();
#pragma warning restore CS0618
        services.AddDbContextPool<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(APIGlobals.ApiConfig.Db.Conn);
            if (APIGlobals.ApiConfig.Db.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });

        services.AddPooledDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(APIGlobals.ApiConfig.Db.Conn);
            if (APIGlobals.ApiConfig.Db.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });


        // ----------------- REDIS -----------------

        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = true,
            Password = APIGlobals.ApiConfig.Redis.Password,
            User = APIGlobals.ApiConfig.Redis.User,
            Ssl = false,
            EndPoints = new EndPointCollection
            {
                { APIGlobals.ApiConfig.Redis.Host, APIGlobals.ApiConfig.Redis.Port }
            }
        };

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IClientAuthService<LinkUser>, ClientAuthService<LinkUser>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();
        services.AddSingleton<IGeoLocation, GeoLocation>();

        var turnStileConfig = APIGlobals.ApiConfig.Turnstile;

        services.AddSingleton(new CloudflareTurnstileOptions
        {
            SecretKey = turnStileConfig.SecretKey ?? string.Empty,
            SiteKey = turnStileConfig.SiteKey ?? string.Empty
        });
        services.AddHttpClient<ICloudflareTurnstileService, CloudflareTurnstileService>();


        // ----------------- MAIL SETUP -----------------
        var emailConfig = APIGlobals.ApiConfig.Mail;
        switch (emailConfig.Type)
        {
            case ApiConfig.MailConfig.MailType.Mailjet:
                if (emailConfig.Mailjet == null)
                    throw new Exception("Mailjet config is null but mailjet is selected as mail type");
                services.AddMailjetEmailService(emailConfig.Mailjet, emailConfig.Sender);
                break;
            case ApiConfig.MailConfig.MailType.Smtp:
                if (emailConfig.Smtp == null)
                    throw new Exception("SMTP config is null but SMTP is selected as mail type");
                services.AddSmtpEmailService(emailConfig.Smtp, emailConfig.Sender, new SmtpServiceTemplates
                {
                    PasswordReset = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/PasswordReset.liquid").Result,
                    EmailVerification = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/EmailVerification.liquid").Result
                });
                break;
            default:
                throw new Exception("Unknown mail type");
        }

        services.AddWebEncoders();
        services.TryAddSingleton<TimeProvider>(provider => TimeProvider.System);
        new AuthenticationBuilder(services)
            .AddScheme<AuthenticationSchemeOptions, LoginSessionAuthentication>(
                OpenShockAuthSchemas.SessionTokenCombo, _ => { })
            .AddScheme<AuthenticationSchemeOptions, DeviceAuthentication>(
                OpenShockAuthSchemas.DeviceToken, _ => { });
        services.AddAuthenticationCore();
        services.AddAuthorization();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.SetIsOriginAllowed(s => true);
                builder.AllowAnyHeader();
                builder.AllowCredentials();
                builder.AllowAnyMethod();
                builder.SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });
        services.AddSignalR()
            .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
            });

        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDeviceUpdateService, DeviceUpdateService>();
        services.AddScoped<IOtaService, OtaService>();
        services.AddScoped<IAccountService, AccountService>();

        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });
        services.AddControllers().AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            x.JsonSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter());
        });

        apiVersioningBuilder.AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        services.AddSwaggerGen(options =>
            {
                options.CustomOperationIds(e =>
                    $"{e.ActionDescriptor.RouteValues["controller"]}_{e.ActionDescriptor.AttributeRouteInfo?.Name ?? e.ActionDescriptor.RouteValues["action"]}");
                options.SchemaFilter<AttributeFilter>();
                options.ParameterFilter<AttributeFilter>();
                options.OperationFilter<AttributeFilter>();
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "OpenShock.API.xml"), true);
                options.AddSecurityDefinition("OpenShockToken", new OpenApiSecurityScheme
                {
                    Name = "OpenShockToken",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyAuth",
                    In = ParameterLocation.Header,
                    Description = "API Token Authorization header."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "OpenShockToken"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                options.AddServer(new OpenApiServer { Url = "https://api.shocklink.net" });
                options.AddServer(new OpenApiServer { Url = "https://dev-api.shocklink.net" });
                options.AddServer(new OpenApiServer { Url = "https://localhost" });
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenShock", Version = "1" });
                options.SwaggerDoc("v2", new OpenApiInfo { Title = "OpenShock", Version = "2" });
                options.MapType<SemVersion>(() => OpenApiSchemas.SemVerSchema);
                options.MapType<PauseReason>(() => OpenApiSchemas.PauseReasonEnumSchema);
            }
        );

        services.ConfigureOptions<ConfigureSwaggerOptions>();
        //services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddHostedService<RedisSubscriberService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory,
        ILogger<Startup> logger)
    {
        ApplicationLogging.LoggerFactory = loggerFactory;
        foreach (var proxy in OpenShockConstants.TrustedProxies)
        {
            var split = proxy.Split('/');
            _forwardedSettings.KnownNetworks.Add(new IPNetwork(IPAddress.Parse(split[0]), int.Parse(split[1])));
        }

        app.UseForwardedHeaders(_forwardedSettings);
        app.UseSerilogRequestLogging();

        app.ConfigureExceptionHandler();

        // global cors policy
        app.UseCors();

        // Redis

        var redisConnection = app.ApplicationServices.GetRequiredService<IRedisConnectionProvider>().Connection;

        redisConnection.CreateIndex(typeof(LoginSession));
        redisConnection.CreateIndex(typeof(DeviceOnline));
        redisConnection.CreateIndex(typeof(DevicePair));
        redisConnection.CreateIndex(typeof(LcgNode));


        if (!APIGlobals.ApiConfig.Db.SkipMigration)
        {
            logger.LogInformation("Running database migrations...");
            using var scope = app.ApplicationServices.CreateScope();
            var openShockContext = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var pendingMigrations = openShockContext.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation("Found pending migrations, applying [{@Migrations}]", pendingMigrations);
                openShockContext.Database.Migrate();
                logger.LogInformation("Applied database migrations... proceeding with startup");
            }
            else logger.LogInformation("No pending migrations found, proceeding with startup");
        }
        else logger.LogWarning("Skipping possible database migrations...");

        app.UseSwagger();
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerUI(c =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
                c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
        });

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<UserHub>("/1/hubs/user",
                options => { options.Transports = HttpTransportType.WebSockets; });
            endpoints.MapHub<ShareLinkHub>("/1/hubs/share/link/{id}",
                options => { options.Transports = HttpTransportType.WebSockets; });
        });
    }
}