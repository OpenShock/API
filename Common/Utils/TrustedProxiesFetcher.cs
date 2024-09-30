namespace OpenShock.Common.Utils;

public static class TrustedProxiesFetcher
{
    private static readonly string[] DefaultProxies = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

    // Fetched 00:00:00 30/09/2024 UTC
    private static readonly string[] PrefetchedCloudflareProxies = [
        // IPv4
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",

        // IPv6
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];

    private static string[] SplitNewLine(string content)
    {
        return content.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static async Task<string[]> FetchCloudflareIPsV4(HttpClient client)
    {
        using var response = await client.GetAsync("https://www.cloudflare.com/ips-v4");
        return SplitNewLine(await response.Content.ReadAsStringAsync());
    }

    private static async Task<string[]> FetchCloudflareIPsV6(HttpClient client)
    {
        using var response = await client.GetAsync("https://www.cloudflare.com/ips-v6");
        return SplitNewLine(await response.Content.ReadAsStringAsync());
    }

    private static async Task<string[]> FetchCloudflareIPs()
    {
        try
        {
            using var client = new HttpClient();

            var v4Task = FetchCloudflareIPsV4(client);
            var v6Task = FetchCloudflareIPsV6(client);

            await Task.WhenAll(v4Task, v6Task);

            return [.. v4Task.Result, .. v6Task.Result];
        }
        catch (Exception)
        {
            return PrefetchedCloudflareProxies;
        }
    }

    private static readonly Task<string[]> CloudflareProxiesTask = FetchCloudflareIPs();

    public static async Task<string[]> GetTrustedProxiesAsync(bool fetch = true)
    {
        string[] cfProxies = fetch ? await CloudflareProxiesTask : PrefetchedCloudflareProxies;

        return [.. DefaultProxies, .. cfProxies];
    }

    public static string[] GetTrustedProxies(bool fetch = true)
    {
        return GetTrustedProxiesAsync(fetch).Result;
    }
}