using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Describes each tag with the &lt;summary&gt; of a controller whose operations carry it, which the built-in XML comment support does
/// not do.
/// </summary>
public sealed class ControllerTagDescriptionTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Tags is not { Count: > 0 } tags) return Task.CompletedTask;

        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var description in context.DescriptionGroups.SelectMany(group => group.Items))
        {
            if (description.ActionDescriptor is not ControllerActionDescriptor controller) continue;

            var summary = XmlDocumentation.Text(XmlDocumentation.Find(controller.ControllerTypeInfo)?.Element("summary"));
            if (summary.Length == 0) continue;

            // Operations are tagged by [Tags] when present, otherwise by controller name
            var tagNames = controller.EndpointMetadata.OfType<ITagsMetadata>().SelectMany(metadata => metadata.Tags).ToArray();
            if (tagNames.Length == 0) tagNames = [controller.ControllerName];

            foreach (var tagName in tagNames) descriptions.TryAdd(tagName, summary);
        }

        foreach (var tag in tags)
        {
            if (tag.Name is not null && tag.Description is null && descriptions.TryGetValue(tag.Name, out var summary)) tag.Description = summary;
        }

        return Task.CompletedTask;
    }
}
