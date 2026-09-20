using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Restores controller class &lt;summary&gt; text as top-level tag descriptions,
/// which Swashbuckle's IncludeXmlComments(..., includeControllerXmlComments: true) used to do.
/// </summary>
public sealed class ControllerTagDescriptionTransformer : IOpenApiDocumentTransformer
{
    private readonly Dictionary<string, string> _summaries;

    public ControllerTagDescriptionTransformer(string xmlPath)
    {
        if (!File.Exists(xmlPath)) throw new FileNotFoundException(xmlPath);
        
        var members = XElement.Load(xmlPath).Element("members")?.Elements("member") ?? [];

        var summaries = new Dictionary<string, string>();

        foreach (var member in members)
        {
            var name = member.Attribute("name")?.Value;
            if (name is null || !name.StartsWith("T:", StringComparison.Ordinal)) continue;

            var summary = member.Element("summary")?.Value.Trim();
            if (string.IsNullOrEmpty(summary)) continue;

            summaries.TryAdd(name, StringUtils.RemoveConsecutiveSpaces(summary));
        }
        
        _summaries = summaries;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var descriptions = new Dictionary<string, string>();

        foreach (var description in context.DescriptionGroups.SelectMany(g => g.Items))
        {
            if (description.ActionDescriptor is not ControllerActionDescriptor controller) continue;

            // Nested/generic types use '+' in reflection but '.' in XML doc IDs
            var key = "T:" + controller.ControllerTypeInfo.FullName?.Replace('+', '.');
            if (!_summaries.TryGetValue(key, out var summary)) continue;

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
