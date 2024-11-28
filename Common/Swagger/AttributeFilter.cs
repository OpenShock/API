using Microsoft.AspNetCore.Authentication;
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
                                .OfType<AuthorizeAttribute>();

        if (attributes?.Any() ?? false)
        {
            var attr = attributes.First();

            // Add what should be show inside the security section
            List<string> securityInfos =
            [
                $"{nameof(AuthorizeAttribute.Policy)}:{attr.Policy}",
                $"{nameof(AuthorizeAttribute.Roles)}:{attr.Roles}",
                $"{nameof(AuthorizeAttribute.AuthenticationSchemes)}:{attr.AuthenticationSchemes}",
            ];

            operation.Security = attr.AuthenticationSchemes switch
            {
                var p when p == OpenShockAuthSchemas.UserSessionCookie => [
                    new OpenApiSecurityRequirement() {{
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = OpenShockAuthSchemas.UserSessionCookie,
                                Type = ReferenceType.SecurityScheme,
                            }
                        },
                        securityInfos
                    }}
                ],
                var p when p == OpenShockAuthSchemas.ApiToken => [
                    new OpenApiSecurityRequirement() {{
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = OpenShockAuthSchemas.ApiToken,
                                Type = ReferenceType.SecurityScheme,
                            }
                        },
                        securityInfos
                    }}
                ],
                var p when p == OpenShockAuthSchemas.HubToken => [
                    new OpenApiSecurityRequirement() {{
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = OpenShockAuthSchemas.HubToken,
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        securityInfos
                    }}
                ],
                _ => [],
            };
        }
        else
        {
            operation.Security.Clear();
        }
    }
}