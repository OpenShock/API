using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
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
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Serialization;
using OpenShock.LiveControlGateway.PubSub;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.ExceptionHandle;
using OpenShock.ServicesCommon.Geo;
using OpenShock.ServicesCommon.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Serilog;
using StackExchange.Redis;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
using JsonSerializer = System.Text.Json.JsonSerializer;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace OpenShock.LiveControlGateway;

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
        LCGGlobals.LCGConfig = configuration.GetChildren().First(x => x.Key.ToLowerInvariant() == "openshock")
                                   .Get<LCGConfig>() ??
                               throw new Exception("Couldnt bind config, check config file");

        var validator = new ValidationContext(LCGGlobals.LCGConfig);
        Validator.ValidateObject(LCGGlobals.LCGConfig, validator, true);

#if DEBUG
        Console.WriteLine(JsonSerializer.Serialize(LCGGlobals.LCGConfig,
            new JsonSerializerOptions { WriteIndented = true }));
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
        services.AddDbContextPool<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(LCGGlobals.LCGConfig.Db);
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        });
        
        services.AddDbContextFactory<OpenShockContext>(builder =>
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

        var redis = new RedisConnectionProvider(_redisConfig);
        redis.Connection.CreateIndex(typeof(LoginSession));
        redis.Connection.CreateIndex(typeof(DeviceOnline));
        redis.Connection.CreateIndex(typeof(DevicePair));
        redis.Connection.CreateIndex(typeof(LcgNode));
        services.AddSingleton<IRedisConnectionProvider>(redis);

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IClientAuthService<LinkUser>, ClientAuthService<LinkUser>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();
        services.AddSingleton<IGeoLocation, GeoLocation>();

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
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "OpenShock.LiveControlGateway.xml"));
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

        services.AddHostedService<LcgKeepAlive>();
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

        PubSubManager.Initialize(ConnectionMultiplexer.Connect(_redisConfig)).Wait();

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
            // endpoints.MapHub<UserHub>("/1/hubs/user",
            //     options => { options.Transports = HttpTransportType.WebSockets; });
            // endpoints.MapHub<ShareLinkHub>("/1/hubs/share/link/{id}",
            //     options => { options.Transports = HttpTransportType.WebSockets; });
        });
    }
}