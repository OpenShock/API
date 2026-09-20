using System.Text.Json.Nodes;
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

        if (methodInfo.IsDefined(typeof(ObsoleteAttribute), true) || methodInfo.DeclaringType?.IsDefined(typeof(ObsoleteAttribute), true) == true)
        {
            operation.Deprecated = true;
        }

        // A default value on a query parameter is exported as a single-value enum (e.g. enum: [""]), which makes clients reject any other value
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter.Schema is OpenApiSchema { Enum.Count: 1, Default: not null } schema && JsonNode.DeepEquals(schema.Enum[0], schema.Default))
            {
                schema.Enum = null;
            }
        }

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

            // API-key schemes must have empty requirement arrays per the OpenAPI spec (only oauth2/openIdConnect use scopes),
            // so roles/policies are surfaced as an extension instead.
            List<string> securityInfos = [];
            if (!string.IsNullOrEmpty(scheme)) securityInfos.Add($"{nameof(AuthorizeAttribute.AuthenticationSchemes)}:{scheme}");
            if (roles.Length > 0) securityInfos.Add($"{nameof(AuthorizeAttribute.Roles)}:{string.Join(',', roles)}");
            if (policies.Length > 0) securityInfos.Add($"{nameof(AuthorizeAttribute.Policy)}:{string.Join(',', policies)}");

            List<OpenApiSecurityRequirement> securityRequirements = [];
            foreach (var authenticationScheme in scheme?.Split(',').Select(s => s.Trim()) ?? [])
            {
                if (authenticationScheme is not (OpenShockAuthSchemes.UserSessionCookie or OpenShockAuthSchemes.ApiToken or OpenShockAuthSchemes.HubToken)) continue;

                securityRequirements.Add(new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference(authenticationScheme, context.Document), [] }
                });
            }

            operation.Security = securityRequirements;

            if (securityInfos.Count > 0)
            {
                operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                operation.Extensions["x-authorization"] = new JsonNodeExtension(new JsonArray(securityInfos.Select(info => (JsonNode)info).ToArray()));
            }
        }
        else
        {
            operation.Security?.Clear();
        }

        return Task.CompletedTask;
    }
}
