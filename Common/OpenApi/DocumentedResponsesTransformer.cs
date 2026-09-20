using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Restores &lt;response code="..."&gt; XML docs as responses, including codes that have no [ProducesResponseType],
/// which Swashbuckle's XML comments filter used to add.
/// </summary>
public sealed class DocumentedResponsesTransformer(DocumentedXmlComments comments) : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor { MethodInfo: var method }) return Task.CompletedTask;

        var codes = comments.FindMember(method)?.Elements("response").Select(r => r.Attribute("code")?.Value);
        if (codes is null) return Task.CompletedTask;

        foreach (var code in codes)
        {
            if (string.IsNullOrEmpty(code)) continue;

            var description = comments.GetResponseDescription(method, code);
            if (description.Length == 0) continue;

            operation.Responses ??= new OpenApiResponses();
            if (operation.Responses.TryGetValue(code, out var existing))
            {
                if (existing is OpenApiResponse response) response.Description = description;
            }
            else
            {
                operation.Responses[code] = new OpenApiResponse { Description = description };
            }
        }

        return Task.CompletedTask;
    }
}
