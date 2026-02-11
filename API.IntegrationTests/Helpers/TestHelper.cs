using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Helpers;

public static class TestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a user directly in DB, creates a session via ISessionService, returns auth info.
    /// This bypasses signup/login endpoints entirely to avoid rate limiting.
    /// </summary>
    public static async Task<AuthenticatedUser> CreateAndLoginUser(
        WebApplicationFactory factory,
        string username,
        string email,
        string password)
    {
        // 1. Create user directly in DB
        var userId = await CreateUserInDb(factory, username, email, password);

        // 2. Create session via ISessionService (stored in Redis)
        await using var scope = factory.Services.CreateAsyncScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var session = await sessionService.CreateSessionAsync(userId, "IntegrationTest", "127.0.0.1");

        return new AuthenticatedUser(userId, username, email, session.Token);
    }

    /// <summary>
    /// Creates an HttpClient that sends the session cookie for authentication.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(WebApplicationFactory factory, string sessionToken)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthConstants.UserSessionCookieName}={sessionToken}");
        return client;
    }

    /// <summary>
    /// Creates an HttpClient that sends an API token header for authentication.
    /// </summary>
    public static HttpClient CreateApiTokenClient(WebApplicationFactory factory, string apiToken)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add(AuthConstants.ApiTokenHeaderName, apiToken);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient that sends a hub/device token header for authentication.
    /// </summary>
    public static HttpClient CreateHubTokenClient(WebApplicationFactory factory, string hubToken)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add(AuthConstants.HubTokenHeaderName, hubToken);
        return client;
    }

    /// <summary>
    /// Creates a user directly in the DB (bypasses signup endpoint).
    /// </summary>
    public static async Task<Guid> CreateUserInDb(
        WebApplicationFactory factory,
        string username,
        string email,
        string password,
        bool activated = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var userId = Guid.CreateVersion7();
        db.Users.Add(new User
        {
            Id = userId,
            Name = username,
            Email = email,
            PasswordHash = HashingUtils.HashPassword(password),
            ActivatedAt = activated ? DateTime.UtcNow : null
        });
        await db.SaveChangesAsync();
        return userId;
    }

    /// <summary>
    /// Creates a device in the DB for a given user. Returns (deviceId, deviceToken).
    /// </summary>
    public static async Task<(Guid DeviceId, string Token)> CreateDeviceInDb(
        WebApplicationFactory factory,
        Guid ownerId,
        string name = "TestDevice")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var deviceId = Guid.CreateVersion7();
        var token = CryptoUtils.RandomAlphaNumericString(256);
        db.Devices.Add(new Device
        {
            Id = deviceId,
            Name = name,
            OwnerId = ownerId,
            Token = token,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (deviceId, token);
    }

    /// <summary>
    /// Creates an API token in the DB for a given user. Returns the raw token string.
    /// </summary>
    public static async Task<(Guid TokenId, string RawToken)> CreateApiTokenInDb(
        WebApplicationFactory factory,
        Guid userId,
        string name = "TestToken",
        List<Common.Models.PermissionType>? permissions = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var rawToken = CryptoUtils.RandomAlphaNumericString(AuthConstants.ApiTokenLength);
        var tokenId = Guid.CreateVersion7();
        db.ApiTokens.Add(new ApiToken
        {
            Id = tokenId,
            UserId = userId,
            Name = name,
            TokenHash = HashingUtils.HashToken(rawToken),
            CreatedByIp = IPAddress.Loopback,
            Permissions = permissions ?? [Common.Models.PermissionType.Shockers_Use]
        });
        await db.SaveChangesAsync();
        return (tokenId, rawToken);
    }

    public static StringContent JsonContent(object obj)
    {
        return new StringContent(JsonSerializer.Serialize(obj, JsonOptions), Encoding.UTF8, "application/json");
    }
}

public sealed record AuthenticatedUser(Guid Id, string Username, string Email, string SessionToken);
