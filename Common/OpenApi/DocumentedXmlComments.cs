using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using Asp.Versioning.OpenApi.Transformers;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// <see cref="XmlComments"/> with its member lookup exposed, so undeclared &lt;response&gt; codes can be enumerated.
/// </summary>
public sealed class DocumentedXmlComments(string path) : XmlComments(path)
{
    private readonly ConcurrentDictionary<MethodInfo, XElement?> _nestedTypeMembers = new();

    public XElement? FindMember(MemberInfo member) => GetMember(member);

    /// <summary>
    /// The library writes nested types as <c>Outer+Inner</c>, but XML documentation ids use <c>Outer.Inner</c>, so a method with a nested
    /// type parameter is never found. For those methods only, falls back to matching by declaring type, name and parameter count
    /// (when that is unambiguous).
    /// </summary>
    protected override XElement? GetMember(MemberInfo member)
    {
        var found = base.GetMember(member);
        if (found is not null || member is not MethodInfo method) return found;

        return HasNestedTypeParameter(method) ? _nestedTypeMembers.GetOrAdd(method, FindByName) : null;
    }

    private XElement? FindByName(MethodInfo method)
    {
        if (method.DeclaringType?.FullName is not { } typeName) return null;

        var prefix = $"M:{typeName.Replace('+', '.')}.{method.Name}";
        var parameterCount = method.GetParameters().Length;

        var matches = Xml.Descendants("member")
            .Where(m => m.Attribute("name")?.Value is { } name && name.StartsWith(prefix, StringComparison.Ordinal) && IsSignature(name, prefix.Length, parameterCount))
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool HasNestedTypeParameter(MethodInfo method) => method.GetParameters().Any(p => ContainsNestedType(p.ParameterType));

    private static bool ContainsNestedType(Type type)
    {
        if (type.IsNested) return true;
        if (type.HasElementType) return ContainsNestedType(type.GetElementType()!);
        return type.IsGenericType && type.GetGenericArguments().Any(ContainsNestedType);
    }

    /// <summary>
    /// Whether the member id continues after <paramref name="nameLength"/> with exactly <paramref name="parameterCount"/> parameters
    /// (or ends there, for a method without any).
    /// </summary>
    private static bool IsSignature(string id, int nameLength, int parameterCount)
    {
        if (id.Length == nameLength) return parameterCount == 0;
        if (id[nameLength] != '(') return false;

        var count = 1;
        var depth = 0;
        foreach (var c in id.AsSpan(nameLength + 1))
        {
            switch (c)
            {
                case '{' or '[': depth++; break;
                case '}' or ']': depth--; break;
                case ',' when depth == 0: count++; break;
            }
        }

        return count == parameterCount;
    }
}
