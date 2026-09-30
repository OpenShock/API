using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Restores controller class &lt;summary&gt; text as top-level tag descriptions,
/// which Swashbuckle's IncludeXmlComments(..., includeControllerXmlComments: true) used to do.
/// </summary>
public sealed class ControllerTagDescriptionTransformer(DocumentedXmlComments comments) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var descriptions = new Dictionary<string, string>();

        foreach (var description in context.DescriptionGroups.SelectMany(g => g.Items))
        {
            if (description.ActionDescriptor is not ControllerActionDescriptor controller) continue;

            var summary = comments.GetSummary(controller.ControllerTypeInfo);
            if (summary.Length == 0) continue;

            // Operations are tagged by [Tags] when present, otherwise by controller name
            var tagNames = controller.EndpointMetadata.OfType<ITagsMetadata>().SelectMany(t => t.Tags).ToArray();
            if (tagNames.Length == 0) tagNames = [controller.ControllerName];

            foreach (var tagName in tagNames)
            {
                descriptions.TryAdd(tagName, summary);
            }
        }

        var tags = new SortedDictionary<string, OpenApiTag>(StringComparer.Ordinal);
        foreach (var tag in document.Tags ?? Enumerable.Empty<OpenApiTag>())
        {
            if (tag.Name is not null) tags.TryAdd(tag.Name, tag);
        }

        // Only describe tags operations actually use (or that already exist); never invent new ones
        foreach (var (name, summary) in descriptions)
        {
            if (tags.TryGetValue(name, out var existing)) existing.Description ??= summary;
        }

        document.Tags = new HashSet<OpenApiTag>(tags.Values);

        return Task.CompletedTask;
    }
}
