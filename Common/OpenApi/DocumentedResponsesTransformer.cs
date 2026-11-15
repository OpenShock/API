using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Adds the &lt;response code="..."&gt; XML docs of codes that have no [ProducesResponseType], which the built-in XML comment support
/// only applies to responses that are already declared.
/// </summary>
public sealed class DocumentedResponsesTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor { MethodInfo: var method }) return Task.CompletedTask;

        var responses = XmlDocumentation.Find(method)?.Elements("response");
        if (responses is null) return Task.CompletedTask;

        foreach (var response in responses)
        {
            if (response.Attribute("code")?.Value is not { Length: > 0 } code) continue;

            var description = XmlDocumentation.Text(response);
            if (description.Length == 0) continue;

            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd(code, new OpenApiResponse { Description = description });
        }

        return Task.CompletedTask;
    }
}
