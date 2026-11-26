using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class SignalRUserHubTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Negotiate endpoint tests (HTTP-based, works with TestServer) ---

    [Test]
    public async Task Negotiate_WithValidSession_ReturnsOk()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneg", "hubneg@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Negotiate_WithApiToken_ReturnsOk()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubnegapi", "hubnegapi@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-negotiate-token");
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Negotiate_WithoutAuth_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Negotiate_WithInvalidSession_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthConstants.UserSessionCookieName}=invalid-session-token");

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Negotiate_WithHubToken_Returns401()
    {
        // Hub/device tokens should NOT work on user hub
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneghub", "hubneghub@test.org", "SecurePassword123#");
        var (_, hubToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "NegTestDevice");
        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, hubToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Negotiate response content ---

    [Test]
    public async Task Negotiate_ReturnsTransportInfo()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneginfo", "hubneginfo@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        // Negotiate response should contain connectionId and available transports
        await Assert.That(doc.RootElement.TryGetProperty("connectionId", out _)).IsTrue();
        await Assert.That(doc.RootElement.TryGetProperty("availableTransports", out _)).IsTrue();
    }

    // --- Public share hub ---

    [Test]
    public async Task PublicShareHub_Negotiate_WithoutAuth_ReturnsOk()
    {
        // PublicShareHub doesn't require auth at the negotiate level
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync($"/1/hubs/share/link/{Guid.CreateVersion7()}/negotiate?negotiateVersion=1", null);
        // Should return OK (negotiate doesn't check auth or share existence)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- An open connection stops working once its session or API token is no longer valid ---

    [Test]
    public async Task Invoke_WithValidSession_KeepsWorking()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevalok"), TestHelper.UniqueEmail("hubrevalok"), "SecurePassword123#");
        await using var connection = await ConnectAsync("Cookie", $"{AuthConstants.UserSessionCookieName}={user.SessionToken}");

        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await Assert.That(connection.State).IsEqualTo(HubConnectionState.Connected);
    }

    [Test]
    public async Task Invoke_AfterSessionDeleted_FailsAndClosesConnection()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevalsess"), TestHelper.UniqueEmail("hubrevalsess"), "SecurePassword123#");
        await using var connection = await ConnectAsync("Cookie", $"{AuthConstants.UserSessionCookieName}={user.SessionToken}");
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISessionService>().DeleteSessionsByUserIdAsync(user.Id);
        }

        await AssertInvokeFailsAndConnectionClosesAsync(connection);
    }

    [Test]
    public async Task Invoke_WithValidApiToken_KeepsWorking()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevaltokok"), TestHelper.UniqueEmail("hubrevaltokok"), "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-revalidate-ok");
        await using var connection = await ConnectAsync(AuthConstants.ApiTokenHeaderName, rawToken);

        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await Assert.That(connection.State).IsEqualTo(HubConnectionState.Connected);
    }

    [Test]
    public async Task Invoke_AfterApiTokenDeleted_FailsAndClosesConnection()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevaltokdel"), TestHelper.UniqueEmail("hubrevaltokdel"), "SecurePassword123#");
        var (tokenId, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-revalidate-deleted");
        await using var connection = await ConnectAsync(AuthConstants.ApiTokenHeaderName, rawToken);
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.ApiTokens.Where(t => t.Id == tokenId).ExecuteDeleteAsync();
        }

        await AssertInvokeFailsAndConnectionClosesAsync(connection);
    }

    [Test]
    public async Task Invoke_AfterApiTokenExpired_FailsAndClosesConnection()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevaltokexp"), TestHelper.UniqueEmail("hubrevaltokexp"), "SecurePassword123#");
        var (tokenId, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-revalidate-expired");
        await using var connection = await ConnectAsync(AuthConstants.ApiTokenHeaderName, rawToken);
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var expired = DateTime.UtcNow.AddMinutes(-1);
            await db.ApiTokens.Where(t => t.Id == tokenId).ExecuteUpdateAsync(s => s.SetProperty(t => t.ValidUntil, expired));
        }

        await AssertInvokeFailsAndConnectionClosesAsync(connection);
    }

    [Test]
    public async Task Invoke_AfterApiTokenOwnerDeactivated_FailsAndClosesConnection()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("hubrevaltokdeact"), TestHelper.UniqueEmail("hubrevaltokdeact"), "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-revalidate-deactivated");
        await using var connection = await ConnectAsync(AuthConstants.ApiTokenHeaderName, rawToken);
        await connection.InvokeAsync("Reboot", Guid.CreateVersion7());

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            db.UserDeactivations.Add(new UserDeactivation
            {
                DeactivatedUserId = user.Id,
                DeactivatedByUserId = user.Id,
                DeleteLater = false
            });
            await db.SaveChangesAsync();
        }

        await AssertInvokeFailsAndConnectionClosesAsync(connection);
    }

    /// <summary>
    /// Opens a real hub connection against the in-process test server. The hub only allows WebSockets, which
    /// the test server offers through its own client rather than through an HttpMessageHandler.
    /// </summary>
    private async Task<HubConnection> ConnectAsync(string headerName, string headerValue)
    {
        var server = WebApplicationFactory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/1/hubs/user"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var client = server.CreateWebSocketClient();
                    client.ConfigureRequest = request => request.Headers[headerName] = headerValue;
                    return await client.ConnectAsync(context.Uri, cancellationToken);
                };
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    private static async Task AssertInvokeFailsAndConnectionClosesAsync(HubConnection connection)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        // The server answers with an error and aborts; which of the two the client sees first is a race.
        Exception? failure = null;
        try
        {
            await connection.InvokeAsync("Reboot", Guid.CreateVersion7());
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        await Assert.That(failure).IsNotNull();

        await closed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await Assert.That(connection.State).IsEqualTo(HubConnectionState.Disconnected);
    }
}
