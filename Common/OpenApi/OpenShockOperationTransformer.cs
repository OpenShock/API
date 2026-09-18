using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenShock.Common.Authentication;
using OpenShock.Common.DataAnnotations.Interfaces;

namespace OpenShock.Common.OpenApi;

public sealed class OpenShockOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var actionDescriptor = context.Description.ActionDescriptor;

        operation.OperationId =
            $"{actionDescriptor.RouteValues["controller"]}_{actionDescriptor.AttributeRouteInfo?.Name ?? actionDescriptor.RouteValues["action"]}";

        if (actionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return Task.CompletedTask;
        }

        var methodInfo = controllerActionDescriptor.MethodInfo;

        // Apply OpenShock Operation Attributes
        foreach (var attribute in methodInfo.GetCustomAttributes(true).OfType<IOperationAttribute>())
        {
            attribute.Apply(operation);
        }

        // Get Authorize attribute
        var attributes = methodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(methodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .ToArray() ?? [];

        if (attributes.Length != 0)
        {
            if (attributes.Count(attr => !string.IsNullOrEmpty(attr.AuthenticationSchemes)) > 1) throw new Exception("Dunno what to apply to this method (multiple authentication attributes with schemes set)");

            var scheme = attributes.Select(attr => attr.AuthenticationSchemes).SingleOrDefault(s => !string.IsNullOrEmpty(s));
            var roles = attributes.Select(attr => attr.Roles).Where(r => !string.IsNullOrEmpty(r)).SelectMany(r => r!.Split(',')).Select(r => r.Trim()).ToArray();
            var policies = attributes.Select(attr => attr.Policy).Where(p => !string.IsNullOrEmpty(p)).SelectMany(p => p!.Split(',')).Select(p => p.Trim()).ToArray();

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
                    OpenShockAuthSchemes.UserSessionCookie => new[]
                    {
                        new OpenApiSecurityRequirement
                        {
                            { new OpenApiSecuritySchemeReference(OpenShockAuthSchemes.UserSessionCookie, context.Document), securityInfos }
                        }
                    },
                    OpenShockAuthSchemes.ApiToken => new[]
                    {
                        new OpenApiSecurityRequirement
                        {
                            { new OpenApiSecuritySchemeReference(OpenShockAuthSchemes.ApiToken, context.Document), securityInfos }
                        }
                    },
                    OpenShockAuthSchemes.HubToken => new[]
                    {
                        new OpenApiSecurityRequirement
                        {
                            { new OpenApiSecuritySchemeReference(OpenShockAuthSchemes.HubToken, context.Document), securityInfos }
                        }
                    },
                    _ => [],
                });
            }

            operation.Security = securityRequirements;
        }
        else
        {
            operation.Security?.Clear();
        }

        return Task.CompletedTask;
    }
}
