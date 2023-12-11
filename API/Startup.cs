using System.Net;
using System.Net.Http.Headers;
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
using OpenShock.API.Hubs;
using OpenShock.API.Mailjet;
using OpenShock.API.Realtime;
using OpenShock.API.Utils;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Serialization;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.ExceptionHandle;
using OpenShock.ServicesCommon.Geo;
using OpenShock.ServicesCommon.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Serilog;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace OpenShock.API;

public class Startup
{
    private ConfigurationOptions _redisConfig;

    private readonly ForwardedHeadersOptions _forwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null
    };

    public Startup(IConfiguration configuration)
    {
#if DEBUG
        var root = (IConfigurationRoot)configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
        APIGlobals.ApiConfig = configuration.GetChildren().First(x => x.Key.ToLowerInvariant() == "openshock")
                                   .Get<ApiConfig>() ??
                               throw new Exception("Couldnt bind config, check config file");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // How do I do this now with EFCore?!
#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ControlType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PermissionType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ShockerModelType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<RankType>();
#pragma warning restore CS0618
        services.AddDbContextPool<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(APIGlobals.ApiConfig.Db);
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        });

        _redisConfig = new ConfigurationOptions
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

        var redisConf = new RedisConfiguration
        {
            AbortOnConnectFail = true,
            Hosts = new[]
            {
                new RedisHost
                {
                    Host = APIGlobals.ApiConfig.Redis.Host,
                    Port = APIGlobals.ApiConfig.Redis.Port
                }
            },
            Database = 0,
            User = APIGlobals.ApiConfig.Redis.User,
            Password = APIGlobals.ApiConfig.Redis.Password
        };

        var redis = new RedisConnectionProvider(_redisConfig);
        redis.Connection.CreateIndex(typeof(LoginSession));
        redis.Connection.CreateIndex(typeof(DeviceOnline));
        redis.Connection.CreateIndex(typeof(DevicePair));
        redis.Connection.CreateIndex(typeof(LcgNode));
        services.AddSingleton<IRedisConnectionProvider>(redis);

        // TODO: Is this needed?
        services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConf);

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IClientAuthService<LinkUser>, ClientAuthService<LinkUser>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();
        services.AddSingleton<IGeoLocation, GeoLocation>();

        services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.mailjet.com/v3.1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{APIGlobals.ApiConfig.Mailjet.Key}:{APIGlobals.ApiConfig.Mailjet.Secret}")));
        });

        services.AddWebEncoders();
        services.TryAddSingleton<TimeProvider>(provider => TimeProvider.System);
#pragma warning disable CS0618 // Type or member is obsolete
        services.TryAddSingleton<ISystemClock, SystemClock>();
#pragma warning restore CS0618 // Type or member is obsolete
        new AuthenticationBuilder(services)
            .AddScheme<LoginSessionAuthenticationSchemeOptions, LoginSessionAuthentication>(
                OpenShockAuthSchemas.SessionTokenCombo, _ => { })
            .AddScheme<DeviceAuthenticationSchemeOptions, DeviceAuthentication>(
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
            .AddOpenShockStackExchangeRedis(options => { options.Configuration = _redisConfig; })
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true; });

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
                    $"{e.ActionDescriptor.RouteValues["controller"]}_{e.ActionDescriptor.RouteValues["action"]}_{e.HttpMethod}");
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
            }
        );

        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddSwaggerGenNewtonsoftSupport();
        //services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        ApplicationLogging.LoggerFactory = loggerFactory;
        var logger = ApplicationLogging.CreateLogger<Startup>();
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

        PubSubManager.Initialize(ConnectionMultiplexer.Connect(_redisConfig), app.ApplicationServices).Wait();

        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        };

        app.UseSwagger();
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerUI(c =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
                c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
        });


        app.UseWebSockets(webSocketOptions);
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            /*endpoints.MapHealthChecks("/{version:apiVersion}/public/healthcheck",
                new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    ResponseWriter = UiResponseWriter.WriteHealthCheckUiResponse
                });*/
            endpoints.MapControllers();
            endpoints.MapHub<UserHub>("/1/hubs/user",
                options => { options.Transports = HttpTransportType.WebSockets; });
            endpoints.MapHub<ShareLinkHub>("/1/hubs/share/link/{id}",
                options => { options.Transports = HttpTransportType.WebSockets; });
        });

        // LucTask.Run(async () =>
        // {
        //     await Task.Delay(10000);
        //     using (var services = app.ApplicationServices.CreateScope())
        //     {
        //         if (!APIGlobals.ApiConfig.SkipDbMigration)
        //         {
        //             logger.LogInformation("Running DB migration...");
        //             var db = services.ServiceProvider.GetRequiredService<ShockLinkContext>();
        //             if ((await db.Database.GetPendingMigrationsAsync()).Any()) await db.Database.MigrateAsync();
        //         }
        //     }
        // });
    }
}