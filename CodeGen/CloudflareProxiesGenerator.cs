using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OpenShock.CodeGen;

[Generator]
internal class CloudflareProxiesGenerator : IIncrementalGenerator
{
    private const string IpV4Name = "cloudflare-ips-v4.txt";
    private const string IpV6Name = "cloudflare-ips-v6.txt";

    private static bool TryParseAndWriteIpNetworkArrayEntry(StringBuilder builder, string ipNetwork, AddressFamily addressFamily)
    {
        if (string.IsNullOrEmpty(ipNetwork)) return false;

        int slash = ipNetwork.IndexOf('/');
        if (slash < 1 || slash == ipNetwork.Length - 1) return false;

        // split into address/CIDR without extra array allocations
        string addressPart = ipNetwork.Substring(0, slash);
        string cidrPart = ipNetwork.Substring(slash + 1);

        if (!ushort.TryParse(cidrPart, out var cidr) || !IPAddress.TryParse(addressPart, out var ipAddress)) return false;

        if (ipAddress.AddressFamily != addressFamily) return false;

        var bytes = ipAddress.GetAddressBytes();

        builder.Append("new IPNetwork(new IPAddress(new byte[]{");

        for (int i = 0, last = bytes.Length - 1; i <= last; i++)
        {
            builder.Append(bytes[i]);
            if (i < last)
                builder.Append(',');
        }

        builder.Append("}),");
        builder.Append(cidr);
        builder.AppendLine("),");

        return true;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var additionalFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(IpV4Name) || file.Path.EndsWith(IpV6Name))
            .Collect();

        context.RegisterSourceOutput(additionalFiles, (ctx, files) =>
        {
            var ipv4Lines = files.FirstOrDefault(f => f.Path.EndsWith(IpV4Name))?.GetText()?.Lines.Select(l => l.ToString().Trim()).Where(l => !string.IsNullOrEmpty(l)) ?? [];
            var ipv6Lines = files.FirstOrDefault(f => f.Path.EndsWith(IpV6Name))?.GetText()?.Lines.Select(l => l.ToString().Trim()).Where(l => !string.IsNullOrEmpty(l)) ?? [];

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;\n");
            sourceBuilder.AppendLine("namespace OpenShock;\n");
            sourceBuilder.AppendLine("public static class CloudflareProxies {");
            sourceBuilder.AppendLine("  public static readonly IPNetwork[] PrefetchedCloudflareProxies = [");
            sourceBuilder.AppendLine("    // IPv4");

            foreach (var line in ipv4Lines)
            {
                if (!TryParseAndWriteIpNetworkArrayEntry(sourceBuilder, line, AddressFamily.InterNetwork))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CF001",
                            "Invalid IPv4 Address",
                            $"The entry '{line}' in {IpV4Name} is not a valid IPv4 Address.",
                            "CloudflareProxiesGenerator",
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
            }
            sourceBuilder.AppendLine("\n    // IPv6");
            foreach (var line in ipv6Lines)
            {
                if (!TryParseAndWriteIpNetworkArrayEntry(sourceBuilder, line, AddressFamily.InterNetworkV6))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CF002",
                            "Invalid IPv6 Address",
                            $"The entry '{line}' in {IpV6Name} is not a valid IPv6 Address.",
                            "CloudflareProxiesGenerator",
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
            }

            sourceBuilder.AppendLine("  ];\n}");

            ctx.AddSource("CloudflareProxies.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        });
    }
}