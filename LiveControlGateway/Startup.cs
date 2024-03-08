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
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.LiveControlGateway.PubSub;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.ExceptionHandle;
using OpenShock.ServicesCommon.Geo;
using OpenShock.ServicesCommon.Services.Device;
using OpenShock.ServicesCommon.Services.Ota;
using OpenShock.ServicesCommon.Services.RedisPubSub;
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
    private readonly ForwardedHeadersOptions _forwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        ForwardedForHeaderName = "CF-Connecting-IP"
    };

    public Startup(IConfiguration configuration)
    {
#if DEBUG
        var root = (IConfigurationRoot)configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
        LCGGlobals.LCGConfig = configuration.GetChildren().First(x => x.Key.Equals("openshock", StringComparison.InvariantCultureIgnoreCase))
                                   .Get<LCGConfig>() ??
                               throw new Exception("Couldn't bind config, check config file");

        var validator = new ValidationContext(LCGGlobals.LCGConfig);
        Validator.ValidateObject(LCGGlobals.LCGConfig, validator, true);

#if DEBUG
        Console.WriteLine(JsonSerializer.Serialize(LCGGlobals.LCGConfig,
            new JsonSerializerOptions { WriteIndented = true }));
#endif
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
        
        // ----------------- REDIS -----------------
        
        var redisConfig = new ConfigurationOptions
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
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IClientAuthService<LinkUser>, ClientAuthService<LinkUser>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();
        services.AddSingleton<IGeoLocation, GeoLocation>();

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
        services.AddScoped<IOtaService, OtaService>();
        

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
        //services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddHostedService<RedisSubscriberService>(); 
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
        
        // Redis
        
        var redisConnection = app.ApplicationServices.GetRequiredService<IRedisConnectionProvider>().Connection;
        
        redisConnection.CreateIndex(typeof(LoginSession));
        redisConnection.CreateIndex(typeof(DeviceOnline));
        redisConnection.CreateIndex(typeof(DevicePair));
        redisConnection.CreateIndex(typeof(LcgNode));
        

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
        });
    }
}