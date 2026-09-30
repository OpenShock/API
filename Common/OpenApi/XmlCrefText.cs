using System.Xml.Linq;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// The library renders a self-closing &lt;see cref="..."/&gt; as nothing, leaving gaps like "An  as exposed to admins".
/// Fills those with the referenced member's name, e.g. <c>T:Ns.EmailOutboxMessage</c> becomes <c>EmailOutboxMessage</c>.
/// </summary>
internal static class XmlCrefText
{
    /// <summary>
    /// Writes a single XML documentation file merging <paramref name="paths"/>, with its crefs filled in, and returns its path.
    /// The caller deletes it.
    /// </summary>
    public static string CreateFilledCopy(IReadOnlyList<string> paths)
    {
        // Whitespace is significant: it is the space between adjacent tags and the margin the library strips
        var xml = XDocument.Load(paths[0], LoadOptions.PreserveWhitespace);

        // The consumers read one file, but types live in several assemblies (e.g. the host and Common)
        var members = xml.Root!.Element("members")!;
        foreach (var path in paths.Skip(1))
        {
            members.Add(XDocument.Load(path, LoadOptions.PreserveWhitespace).Root?.Element("members")?.Elements() ?? []);
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
        try
        {
            xml.Save(copy, SaveOptions.DisableFormatting);
        }
        catch
        {
            File.Delete(copy);
            throw;
        }

        return copy;
    }
}
