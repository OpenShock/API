using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Restores &lt;response code="..."&gt; XML docs as responses, including codes that have no [ProducesResponseType],
/// which Swashbuckle's XML comments filter used to add.
/// </summary>
public sealed class DocumentedResponsesTransformer : IOpenApiOperationTransformer
{
    private readonly Dictionary<string, (string Code, string Description)[]> _responses = new();

    public DocumentedResponsesTransformer(string xmlPath)
    {
        var members = XElement.Load(xmlPath).Element("members")?.Elements("member") ?? [];

        foreach (var member in members)
        {
            var name = member.Attribute("name")?.Value;
            if (name is null || !name.StartsWith("M:", StringComparison.Ordinal)) continue;

            var responses = member.Elements("response")
                .Select(r => (Code: r.Attribute("code")?.Value, Description: string.Join(' ', r.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))))
                .Where(r => !string.IsNullOrEmpty(r.Code) && r.Description.Length > 0)
                .Select(r => (r.Code!, r.Description))
                .ToArray();

            if (responses.Length > 0) _responses.TryAdd(name, responses);
        }
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor { MethodInfo: var method }) return Task.CompletedTask;
        if (!_responses.TryGetValue(GetMemberId(method), out var documented)) return Task.CompletedTask;

        operation.Responses ??= new OpenApiResponses();
        foreach (var (code, description) in documented)
        {
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

    /// <summary>
    /// Builds the XML documentation member id of a method, e.g. <c>M:Ns.Type.Method(System.Guid,System.Collections.Generic.List{System.String})</c>.
    /// </summary>
    private static string GetMemberId(MethodInfo method)
    {
        var sb = new StringBuilder("M:");
        sb.Append(method.DeclaringType!.FullName!.Replace('+', '.')).Append('.').Append(method.Name);

        if (method.IsGenericMethodDefinition) sb.Append("``").Append(method.GetGenericArguments().Length);

        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            sb.Append('(').AppendJoin(',', parameters.Select(p => GetTypeId(p.ParameterType))).Append(')');
        }

        return sb.ToString();
    }

    private static string GetTypeId(Type type)
    {
        if (type.IsByRef) return GetTypeId(type.GetElementType()!) + "@";
        if (type.IsArray) return GetTypeId(type.GetElementType()!) + (type.GetArrayRank() == 1 ? "[]" : "[" + string.Join(',', Enumerable.Repeat("0:", type.GetArrayRank())) + "]");
        if (type.IsGenericParameter) return (type.DeclaringMethod is null ? "`" : "``") + type.GenericParameterPosition;

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().FullName!;
            return name[..name.IndexOf('`')].Replace('+', '.') + "{" + string.Join(',', type.GetGenericArguments().Select(GetTypeId)) + "}";
        }

        return type.FullName!.Replace('+', '.');
    }
}
