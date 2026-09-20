using System.Reflection;
using System.Xml.Linq;
using Asp.Versioning.OpenApi.Transformers;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// <see cref="XmlComments"/> with its member lookup exposed, so undeclared &lt;response&gt; codes can be enumerated.
/// </summary>
public sealed class DocumentedXmlComments(string path) : XmlComments(path)
{
    public XElement? FindMember(MemberInfo member) => GetMember(member);

    /// <summary>
    /// The library writes nested types as <c>Outer+Inner</c>, but XML documentation ids use <c>Outer.Inner</c>, so a method with a nested
    /// type parameter is never found. Falls back to matching the method by declaring type and name (only when that is unambiguous).
    /// </summary>
    protected override XElement? GetMember(MemberInfo member)
    {
        var found = base.GetMember(member);
        if (found is not null || member is not MethodInfo { DeclaringType.FullName: { } typeName } method) return found;

        var prefix = $"M:{typeName.Replace('+', '.')}.{method.Name}";
        var matches = Xml.Descendants("member")
            .Where(m => m.Attribute("name")?.Value is { } name && name.StartsWith(prefix, StringComparison.Ordinal) && (name.Length == prefix.Length || name[prefix.Length] == '('))
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }
}

/// <summary>
/// The library renders a self-closing &lt;see cref="..."/&gt; as nothing, leaving gaps like "An  as exposed to admins".
/// Fills those with the referenced member's name, e.g. <c>T:Ns.EmailOutboxMessage</c> becomes <c>EmailOutboxMessage</c>.
/// </summary>
public static class XmlCrefText
{
    /// <summary>
    /// Writes a single XML documentation file merging <paramref name="paths"/>, with its crefs filled in, and returns its path.
    /// The caller deletes it.
    /// </summary>
    public static string CreateFilledCopy(params string[] paths)
    {
        var xml = XDocument.Load(paths[0]);

        // The consumers read one file, but types live in several assemblies (e.g. the host and Common)
        var members = xml.Root!.Element("members")!;
        foreach (var path in paths.Skip(1))
        {
            members.Add(XDocument.Load(path).Root?.Element("members")?.Elements() ?? []);
        }

        foreach (var see in xml.Descendants("see"))
        {
            if (!see.IsEmpty || see.Attribute("cref")?.Value is not { Length: > 2 } cref) continue;

            // Drop the "T:"/"M:"/... prefix and any parameter list, then keep the last segment
            var name = cref[2..];
            var parameters = name.IndexOf('(');
            if (parameters >= 0) name = name[..parameters];

            see.Value = name[(name.LastIndexOf('.') + 1)..];
        }

        var copy = Path.GetTempFileName();
        xml.Save(copy);
        return copy;
    }
}
