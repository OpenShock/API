using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.AuthenticationHandlers;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.ExceptionHandle;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.OpenApi;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenTelemetry.Metrics;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Common;

public static class OpenShockServiceHelper
{
    public static DatabaseOptions GetDatabaseOptions(this ConfigurationManager configuration)
    {
        var section = configuration.GetRequiredSection(DatabaseOptions.SectionName);
        if (!section.Exists()) throw new Exception("TODO");

        return section.Get<DatabaseOptions>() ?? throw new Exception("TODO");
    }

    public static ConfigurationOptions GetRedisConfigurationOptions(this ConfigurationManager configuration)
    {
        var section = configuration.GetRequiredSection(RedisOptions.SectionName);
        if (!section.Exists()) throw new Exception("TODO");

        var options = section.Get<RedisOptions>() ?? throw new Exception("TODO");


        ConfigurationOptions configurationOptions;

        if (string.IsNullOrWhiteSpace(options.Conn))
        {
            configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = true,
                Password = options.Password,
                User = options.User,
                Ssl = false,
                EndPoints = new EndPointCollection
                {
                    { options.Host, options.Port }
                }
            };
        }
        else
        {
            configurationOptions = ConfigurationOptions.Parse(options.Conn);
        }

        configurationOptions.AbortOnConnectFail = true;

        return configurationOptions;
    }

    /// <summary>
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <param name="databaseOptions"></param>
    /// <param name="redisConfigurationOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenShockServices(this IServiceCollection services, DatabaseOptions databaseOptions, ConfigurationOptions redisConfigurationOptions)
    {
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
            options.AddPolicy(OpenShockAuthPolicies.RankAdmin, policy => policy.RequireRole("Admin", "System"));
            // TODO: Add token permission policies
        });
        
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, OpenShockAuthorizationMiddlewareResultHandler>();
        
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

        services.AddOpenApi("1", options =>
        {
            // Always inline enum schemas
            options.CreateSchemaReferenceId = (type) =>
                type.Type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);

            options.AddDocumentTransformer<OpenApiDocumentTransformer>();
            options.AddOperationTransformer<OpenApiOperationTransformer>();
            options.AddSchemaTransformer<OpenApiSchemaTransformer>();
        });
        services.AddOpenApi("2", options =>
        {
            // Always inline enum schemas
            options.CreateSchemaReferenceId = (type) =>
                type.Type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);

            options.AddDocumentTransformer<OpenApiDocumentTransformer>();
            options.AddOperationTransformer<OpenApiOperationTransformer>();
            options.AddSchemaTransformer<OpenApiSchemaTransformer>();
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
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfigurationOptions));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();

        // <---- Postgres EF Core ---->
        services.AddDbContextPool<OpenShockContext>(builder => OpenShockContext.ConfigureOptionsBuilder(builder, databaseOptions.Conn, databaseOptions.Debug));
        services.AddPooledDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(databaseOptions.Conn);
            if (databaseOptions.Debug)
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

        return services;
    }

    public readonly record struct ServicesResult(ConfigurationOptions RedisConfig);
}