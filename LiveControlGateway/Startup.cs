using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.PubSub;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Startup class for the LCG
/// </summary>
public sealed class Startup
{
    private LCGConfig _lcgConfig;

    /// <summary>
    /// Setup the LCG, configure config and validate
    /// </summary>
    /// <param name="configuration"></param>
    /// <exception cref="Exception"></exception>
    public Startup(IConfiguration configuration)
    {
#if DEBUG
        var root = (IConfigurationRoot)configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
        _lcgConfig = configuration.GetChildren().First(x => x.Key.Equals("openshock", StringComparison.InvariantCultureIgnoreCase))
                         .Get<LCGConfig>() ??
                     throw new Exception("Couldn't bind config, check config file");

        var validator = new ValidationContext(_lcgConfig);
        Validator.ValidateObject(_lcgConfig, validator, true);

#if DEBUG
        Console.WriteLine(JsonSerializer.Serialize(_lcgConfig,
            new JsonSerializerOptions { WriteIndented = true }));
#endif
    }

    /// <summary>
    /// Configures the services for the LCG
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_lcgConfig);
        
        var commonService = services.AddOpenShockServices(_lcgConfig);
        
        services.AddSignalR()
            .AddOpenShockStackExchangeRedis(options => { options.Configuration = commonService.RedisConfig; })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
            });

        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IOtaService, OtaService>();
        
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
                                Id = AuthConstants.AuthTokenHeaderName
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                options.AddServer(new OpenApiServer { Url = "https://api.openshock.app" });
                options.AddServer(new OpenApiServer { Url = "https://staging-api.openshock.app" });
                options.AddServer(new OpenApiServer { Url = "https://localhost" });
            }
        );

        services.ConfigureOptions<ConfigureSwaggerOptions>();
        //services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddHostedService<RedisSubscriberService>(); 
        services.AddHostedService<LcgKeepAlive>();

        services.AddSingleton<HubLifetimeManager>();

    }

    /// <summary>
    /// Register middleware and co.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <param name="loggerFactory"></param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseCommonOpenShockMiddleware();

        app.UseSwagger();
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerUI(c =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
                c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
        });
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}