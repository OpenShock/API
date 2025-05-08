using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Net;
using System.Text;

namespace OpenShock.CodeGen;

[Generator]
internal class CloudflareProxiesGenerator : IIncrementalGenerator
{
    private const string IpV4Name = "cloudflare-ips-v4.txt";
    private const string IpV6Name = "cloudflare-ips-v6.txt";

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
                if (!IPAddress.TryParse(line, out _)) throw new Exception("Invalid IP Address in Cloudflare IPv4 Proxy list!");

                sourceBuilder.Append("    IPNetwork.Parse(\"");
                sourceBuilder.Append(line);
                sourceBuilder.AppendLine("\"),");
            }
            sourceBuilder.AppendLine("\n    // IPv6");
            foreach (var line in ipv6Lines)
            {
                if (!IPAddress.TryParse(line, out _)) throw new Exception("Invalid IP Address in Cloudflare IPv6 Proxy list!");

                sourceBuilder.Append("    IPNetwork.Parse(\"");
                sourceBuilder.Append(line);
                sourceBuilder.AppendLine("\"),");
            }

            sourceBuilder.AppendLine("  ];\n}");

            ctx.AddSource("CloudflareProxies.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        });
    }
}