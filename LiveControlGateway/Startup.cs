using System.Net;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.Common.Serialization;
using OpenShock.Common.ShockLinkDb;
using OpenShock.LiveControlGateway.Services;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.ExceptionHandle;
using OpenShock.ServicesCommon.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Serilog;
using StackExchange.Redis;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace OpenShock.LiveControlGateway;

public class Startup
{
    public static string EnvString { get; private set; } = null!;
    private ConfigurationOptions _redisConfig;

    private readonly ForwardedHeadersOptions _forwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null
    };

    public Startup(IConfiguration configuration)
    {
        LCGGlobals.LCGConfig = configuration.GetChildren().First(x => x.Key == "ShockLink").Get<LCGConfig>() ??
                               throw new Exception("Couldnt bind config, check config file");
#if DEBUG
        var root = (IConfigurationRoot)configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
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
        services.AddDbContextPool<ShockLinkContext>(builder =>
        {
            builder.UseNpgsql(LCGGlobals.LCGConfig.Db);
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        });
        
        _redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = true,
            Password = LCGGlobals.LCGConfig.Redis.Password,
            User = LCGGlobals.LCGConfig.Redis.User,
            Ssl = false,
            EndPoints = new EndPointCollection
            {
                { LCGGlobals.LCGConfig.Redis.Host, LCGGlobals.LCGConfig.Redis.Port }
            }
        };

        // var redisConf = new RedisConfiguration
        // {
        //     AbortOnConnectFail = true,
        //     Hosts = new[]
        //     {
        //         new RedisHost
        //         {
        //             Host = APIGlobals.ApiConfig.Redis.Host,
        //             Port = APIGlobals.ApiConfig.Redis.Port
        //         }
        //     },
        //     Database = 0,
        //     User = APIGlobals.ApiConfig.Redis.User,
        //     Password = APIGlobals.ApiConfig.Redis.Password
        // };

        services.AddGrpc();
        
        var redis = new RedisConnectionProvider(_redisConfig);
        redis.Connection.CreateIndex(typeof(LoginSession));
        redis.Connection.CreateIndex(typeof(DeviceOnline));
        redis.Connection.CreateIndex(typeof(DevicePair));
        services.AddSingleton<IRedisConnectionProvider>(redis);

        // TODO: Is this needed?
        //services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConf);

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IClientAuthService<LinkUser>, ClientAuthService<LinkUser>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();

        services.AddWebEncoders();
        services.TryAddSingleton<ISystemClock, SystemClock>();
        new AuthenticationBuilder(services)
            .AddScheme<LoginSessionAuthenticationSchemeOptions, LoginSessionAuthentication>(
                ShockLinkAuthSchemas.SessionTokenCombo, _ => { })
            .AddScheme<DeviceAuthenticationSchemeOptions, DeviceAuthentication>(
                ShockLinkAuthSchemas.DeviceToken, _ => { });
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
            .AddStackExchangeRedis(options => { options.Configuration = _redisConfig; })
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
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "ShockLink.API.xml"));
                options.AddSecurityDefinition("ShockLinkToken", new OpenApiSecurityScheme
                {
                    Name = "ShockLinkToken",
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
                                Id = "ShockLinkToken"
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
        EnvString = env.EnvironmentName;
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

        //PubSubManager.Initialize(ConnectionMultiplexer.Connect(_redisConfig), app.ApplicationServices);

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
            endpoints.MapGrpcService<GreeterService>();
            // endpoints.MapHub<UserHub>("/1/hubs/user",
            //     options => { options.Transports = HttpTransportType.WebSockets; });
            // endpoints.MapHub<ShareLinkHub>("/1/hubs/share/link/{id}",
            //     options => { options.Transports = HttpTransportType.WebSockets; });
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