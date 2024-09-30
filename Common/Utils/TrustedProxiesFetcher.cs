namespace OpenShock.Common.Utils;

public static class TrustedProxiesFetcher
{
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
        using var client = new HttpClient();

        var v4Task = FetchCloudflareIPsV4(client);
        var v6Task = FetchCloudflareIPsV6(client);

        await Task.WhenAll(v4Task, v6Task);

        return [.. v4Task.Result, .. v6Task.Result];
    }

    private static readonly string[] DefaultProxies = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];
    private static readonly Task<string[]> CloudflareProxiesTask = FetchCloudflareIPs();

    public static async Task<string[]> GetTrustedProxies()
    {
        return [.. DefaultProxies, .. await CloudflareProxiesTask];
    }
}