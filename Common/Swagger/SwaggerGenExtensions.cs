﻿using Microsoft.OpenApi.Models;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations;
using OpenShock.Common.Models;
using Semver;
using Asp.Versioning.ApiExplorer;
using OpenShock.Common.Utils;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Linq;
using OpenShock.Common.Extensions;
using OpenShock.Common.Authentication;

namespace OpenShock.Common.Swagger;

public static class SwaggerGenExtensions
{
    public static IServiceCollection AddSwaggerExt<TProgram>(this IServiceCollection services) where TProgram : class
    {
        var assembly = typeof(TProgram).Assembly;

        string assemblyName = assembly
                                .GetName()
                                .Name ?? throw new NullReferenceException("Assembly name");

        var versions = assembly.GetAllControllerEndpointAttributes<ApiVersionAttribute>()
                        .SelectMany(type => type.Versions)
                        .Select(v => v.ToString())
                        .ToHashSet()
                        .OrderBy(v => v)
                        .ToArray();

        if (versions.Any(v => !int.TryParse(v, out _)))
        {
            throw new InvalidDataException($"Found invalid API versions: [{string.Join(", ", versions.Where(v => !int.TryParse(v, out _)))}]");
        }

        return services
            .AddSwaggerGen(options =>
            {
                options.CustomOperationIds(e =>
                    $"{e.ActionDescriptor.RouteValues["controller"]}_{e.ActionDescriptor.AttributeRouteInfo?.Name ?? e.ActionDescriptor.RouteValues["action"]}");
                options.SchemaFilter<AttributeFilter>();
                options.ParameterFilter<AttributeFilter>();
                options.OperationFilter<AttributeFilter>();
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, assemblyName + ".xml"), true);
                options.AddSecurityDefinition(OpenShockAuthSchemas.UserSessionCookie, new OpenApiSecurityScheme
                {
                    Name = AuthConstants.UserSessionCookieName,
                    Description = "Enter user session cookie",
                    In = ParameterLocation.Cookie,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemas.UserSessionCookie,
                    Reference = new OpenApiReference
                    {
                        Id = OpenShockAuthSchemas.UserSessionCookie,
                        Type = ReferenceType.SecurityScheme,
                    }
                });
                options.AddSecurityDefinition(OpenShockAuthSchemas.ApiToken, new OpenApiSecurityScheme
                {
                    Name = AuthConstants.ApiTokenHeaderName,
                    Description = "Enter API Token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemas.ApiToken,
                    Reference = new OpenApiReference
                    {
                        Id = OpenShockAuthSchemas.ApiToken,
                        Type = ReferenceType.SecurityScheme,
                    }
                });
                options.AddSecurityDefinition(OpenShockAuthSchemas.HubToken, new OpenApiSecurityScheme
                {
                    Name = AuthConstants.HubTokenHeaderName,
                    Description = "Enter hub token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemas.HubToken,
                    Reference = new OpenApiReference
                    {
                        Id = OpenShockAuthSchemas.HubToken,
                        Type = ReferenceType.SecurityScheme,
                    }
                });
                options.AddServer(new OpenApiServer { Url = "https://api.openshock.app" });
                options.AddServer(new OpenApiServer { Url = "https://api.openshock.dev" });
#if DEBUG
                options.AddServer(new OpenApiServer { Url = "https://localhost" });
#endif
                foreach (var version in versions)
                {
                    options.SwaggerDoc("v" + version, new OpenApiInfo { Title = "OpenShock", Version = version });
                }
                options.MapType<SemVersion>(() => OpenApiSchemas.SemVerSchema);
                options.MapType<PauseReason>(() => OpenApiSchemas.PauseReasonEnumSchema);

                // Avoid nullable strings everywhere
                options.SupportNonNullableReferenceTypes();
            })
            .ConfigureOptions<ConfigureSwaggerOptions>();
    }

    public static IApplicationBuilder UseSwaggerExt(this WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var groupNames = provider.ApiVersionDescriptions.Select(d => d.GroupName).ToArray();

        return app
            .UseSwagger()
            .UseSwaggerUI(c =>
        {
            foreach (var groupName in groupNames)
            {
                c.SwaggerEndpoint($"/swagger/{groupName}/swagger.json", groupName.ToUpperInvariant());
            }
        });
    }
}
