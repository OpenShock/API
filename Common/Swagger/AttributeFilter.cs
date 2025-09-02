using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using OpenShock.Common.Authentication;
using OpenShock.Common.DataAnnotations.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpenShock.Common.Swagger;

public sealed class AttributeFilter : ISchemaFilter, IParameterFilter, IOperationFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        // Apply OpenShock Parameter Attributes
        foreach (var attribute in context.ParameterInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ?? [])
        {
            attribute.Apply(parameter);
        }

        // Apply OpenShock Parameter Attributes
        foreach (var attribute in context.PropertyInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ?? [])
        {
            attribute.Apply(parameter);
        }
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Apply OpenShock Parameter Attributes
        foreach (var attribute in context.MemberInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ?? [])
        {
            attribute.Apply(schema);
        }
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Apply OpenShock Parameter Attributes
        foreach (var attribute in context.MethodInfo?.GetCustomAttributes(true).OfType<IOperationAttribute>() ?? [])
        {
            attribute.Apply(operation);
        }

        // Get Authorize attribute
        var attributes = context.MethodInfo?.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .ToArray() ?? [];

        if (attributes.Length != 0)
        {
            if (attributes.Count(attr => !string.IsNullOrEmpty(attr.AuthenticationSchemes)) > 1) throw new Exception("Dunno what to apply to this method (multiple authentication attributes with schemes set)");
            
            var scheme = attributes.Select(attr => attr.AuthenticationSchemes).SingleOrDefault(scheme => !string.IsNullOrEmpty(scheme));
            var roles = attributes.Select(attr => attr.Roles).Where(roles => !string.IsNullOrEmpty(roles)).SelectMany(roles => roles!.Split(',')).Select(role => role.Trim()).ToArray();
            var policies = attributes.Select(attr => attr.Policy).Where(policies => !string.IsNullOrEmpty(policies)).SelectMany(policies => policies!.Split(',')).Select(policy => policy.Trim()).ToArray();

            // Add what should be show inside the security section
            List<string> securityInfos = [];
            if (!string.IsNullOrEmpty(scheme)) securityInfos.Add($"{nameof(AuthorizeAttribute.AuthenticationSchemes)}:{scheme}");
            if (roles.Length > 0) securityInfos.Add($"{nameof(AuthorizeAttribute.Roles)}:{string.Join(',', roles)}");
            if (policies.Length > 0) securityInfos.Add($"{nameof(AuthorizeAttribute.Policy)}:{string.Join(',', policies)}");

            List<OpenApiSecurityRequirement> securityRequirements = [];
            foreach (var authenticationScheme in scheme?.Split(',').Select(s => s.Trim()) ?? [])
            {
                securityRequirements.AddRange(authenticationScheme switch
                {
                    OpenShockAuthSchemes.UserSessionCookie => [
                        new OpenApiSecurityRequirement {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = OpenShockAuthSchemes.UserSessionCookie,
                                    Type = ReferenceType.SecurityScheme,
                                }
                            },
                            securityInfos
                        }}
                    ],
                    OpenShockAuthSchemes.ApiToken => [
                        new OpenApiSecurityRequirement {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = OpenShockAuthSchemes.ApiToken,
                                    Type = ReferenceType.SecurityScheme,
                                }
                            },
                            securityInfos
                        }}
                    ],
                    OpenShockAuthSchemes.HubToken => [
                        new OpenApiSecurityRequirement {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = OpenShockAuthSchemes.HubToken,
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            securityInfos
                        }}
                    ],
                    _ => [],
                });
            }

            operation.Security = securityRequirements;
        }
        else
        {
            operation.Security.Clear();
        }
    }
}
