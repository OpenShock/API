using System.Net;

namespace OpenShock.Common.Utils;

public static class TrustedProxiesFetcher
{
    private static readonly HttpClient Client = new();

    public static readonly string[] PrivateNetworks =
    [
        // Loopback
        "127.0.0.0/8",
        "::1/128",
        "::ffff:127.0.0.0/8",
        
        // Private IPv4
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",

        // Private IPv6
        "fc00::/7",
        "fe80::/10",
    ];

    private static readonly IPNetwork[] PrivateNetworksParsed = [.. PrivateNetworks.Select(IPNetwork.Parse)];

    private static readonly char[] NewLineSeperators = ['\r', '\n', '\t'];
    
    private static async Task<IReadOnlyList<IPNetwork>> FetchCloudflareIPs(Uri uri, CancellationToken ct)
    {
        using var response = await Client.GetAsync(uri, ct);
        var stringResponse = await response.Content.ReadAsStringAsync(ct);
        
        return ParseNetworks(stringResponse);
    }

    private static IPNetwork[] ParseNetworks(string response)
    {
        var lines = response.Split(NewLineSeperators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var networks = new IPNetwork[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            networks[i] = IPNetwork.Parse(lines[i]);
        }

        return networks;
    }
    
    private static async Task<IPNetwork[]?> FetchCloudflareIPs()
    {
        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5)); // Don't want to make application startup slow
            var ct = cts.Token;
            
            var v4Task = FetchCloudflareIPs(new Uri("https://www.cloudflare.com/ips-v4"), ct);
            var v6Task = FetchCloudflareIPs(new Uri("https://www.cloudflare.com/ips-v6"), ct);

            await Task.WhenAll(v4Task, v6Task);

            return [.. v4Task.Result, .. v6Task.Result];
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<IPNetwork[]> GetTrustedNetworksAsync(bool fetch = true)
    {
        IPNetwork[]? cfProxies = null;

        if (fetch)
        {
            cfProxies = await FetchCloudflareIPs();
        }

        if (cfProxies is null)
        {
            var assembly = typeof(TrustedProxiesFetcher).Assembly;
            var resourceName = assembly.GetName().Name + ".cloudflare-ips.txt";
            await using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new NullReferenceException("Could not open embedded cloudflare-ips.txt file");
            using var reader = new StreamReader(stream);
            cfProxies = ParseNetworks(await reader.ReadToEndAsync());
        }

        return [.. PrivateNetworksParsed, .. cfProxies];
    }
}