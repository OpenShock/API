using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.AuthenticationHandlers;
using OpenShock.Common.Authentication.Requirements;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Config;
using OpenShock.Common.ExceptionHandle;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenTelemetry.Metrics;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;

namespace OpenShock.Common;

public static class OpenShockServiceHelper
{
    /// <summary>
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static ServicesResult AddOpenShockServices(this IServiceCollection services, BaseConfig config)
    {
        services.AddSingleton<BaseConfig>();
        
        // <---- ASP.NET ---->
        services.AddExceptionHandler<OpenShockExceptionHandler>();
        
        services.AddScoped<IClientAuthService<User>, ClientAuthService<User>>();
        services.AddScoped<IClientAuthService<Device>, ClientAuthService<Device>>();
        services.AddScoped<IUserReferenceService, UserReferenceService>();

        services.AddAuthenticationCore();
        new AuthenticationBuilder(services)
            .AddScheme<AuthenticationSchemeOptions, UserSessionAuthentication>(
                OpenShockAuthSchemas.UserSessionCookie, _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthentication>(
                OpenShockAuthSchemas.ApiToken, _ => { })
            .AddScheme<AuthenticationSchemeOptions, HubAuthentication>(
                OpenShockAuthSchemas.HubToken, _ => { });
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy(OpenShockAuthPolicies.TokenOnly, policy => policy.RequireClaim(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemas.ApiToken));
            options.AddPolicy(OpenShockAuthPolicies.UserOnly, policy => policy.RequireClaim(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemas.UserSessionCookie));
            
            options.AddPolicy(OpenShockAuthPolicies.RankAdmin, policy => policy.RequireRole("Admin", "System"));
            // TODO: Add token permission policies
        });
        
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new PermissionTypeConverter());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        
        services.AddControllers().AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            x.JsonSerializerOptions.Converters.Add(new PermissionTypeConverter());
            x.JsonSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter());
        });
        
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        apiVersioningBuilder.AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "VVV";
            setup.SubstituteApiVersionInUrl = true;
            setup.DefaultApiVersion = new ApiVersion(1, 0);
            setup.AssumeDefaultVersionWhenUnspecified = true;
        });
        
        // generic ASP.NET stuff
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddWebEncoders();
        services.AddProblemDetails();
        services.TryAddSingleton<TimeProvider>(provider => TimeProvider.System);
        
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
        
        // This needs to be at this position, earlier will break validation error responses
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblem(context.ModelState);
                return problemDetails.ToObjectResult(context.HttpContext);
            };
        });
        
        // OpenTelemetry

        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());
        
        // <---- Redis ---->
        
        ConfigurationOptions configurationOptions;

        if (string.IsNullOrWhiteSpace(config.Redis.Conn))
        {
            if (string.IsNullOrWhiteSpace(config.Redis.Host))
                throw new Exception("You need to specify either OPENSHOCK__REDIS__CONN or OPENSHOCK__REDIS__HOST");

            configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = true,
                Password = config.Redis.Password,
                User = config.Redis.User,
                Ssl = false,
                EndPoints = new EndPointCollection
                {
                    { config.Redis.Host, config.Redis.Port }
                }
            };
        }
        else
        {
            configurationOptions = ConfigurationOptions.Parse(config.Redis.Conn);
        }

        configurationOptions.AbortOnConnectFail = true;

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configurationOptions));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();
        
        // <---- Postgres EF Core ---->
        
        services.AddDbContextPool<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(config.Db.Conn, optionsBuilder =>
            {
                optionsBuilder.MapEnum<RoleType>();
                optionsBuilder.MapEnum<ControlType>();
                optionsBuilder.MapEnum<PermissionType>();
                optionsBuilder.MapEnum<ShockerModelType>();
                optionsBuilder.MapEnum<OtaUpdateStatus>();
            });
            if (config.Db.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });

        services.AddPooledDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(config.Db.Conn);
            if (config.Db.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });
        
        // <---- OpenShock Services ---->

        services.AddScoped<ISessionService, SessionService>();
        services.AddSingleton<IBatchUpdateService, BatchUpdateService>();
        services.AddHostedService<BatchUpdateService>(provider =>
            (BatchUpdateService)provider.GetRequiredService<IBatchUpdateService>());
        
        return new ServicesResult
        {
            RedisConfig = configurationOptions
        };
    }

    public readonly record struct ServicesResult(ConfigurationOptions RedisConfig);
}