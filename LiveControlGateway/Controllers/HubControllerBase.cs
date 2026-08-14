using System.Diagnostics.CodeAnalysis;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Results;
using OpenShock.Common.Utils;
using OpenShock.Common.Websocket;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization.Gateway;
using System.Net.WebSockets;
using System.Security.Claims;
using OpenShock.Common.Authentication;
using OpenShock.Common.Extensions;
using SemVersion = OpenShock.Common.Models.SemVersion;
using Timer = System.Timers.Timer;

using OpenShock.Internal.Common.Problems;

using OpenShock.Internal.Common.Errors;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Basis for the hub controllers, shares common functionality
/// </summary>
/// <typeparam name="TIn">The type we are receiving / deserializing</typeparam>
/// <typeparam name="TOut">The type we are sending out / serializing</typeparam>
public abstract class HubControllerBase<TIn, TOut> : FlatbuffersWebsocketBaseController<TIn, TOut>, IHubController, IActionFilter where TIn : class, IFlatBufferSerializable where TOut : class, IFlatBufferSerializable
{
    /// <summary>
    /// The current hub
    /// </summary>
    protected Guid CurrentHubId = Guid.Empty;
    protected Guid CurrentHubOwnerId = Guid.Empty;
    
    /// <summary>
    /// Service provider
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Hub lifetime
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [field: AllowNull, MaybeNull]
    protected HubLifetime HubLifetime
    {
        get => field ?? throw new InvalidOperationException("Hub lifetime is null but was tried to access");
        private set;
    }

    private readonly LcgOptions _options;

    private readonly HubLifetimeManager _hubLifetimeManager;

    private readonly Timer _keepAliveTimeoutTimer = new(Duration.DeviceKeepAliveInitialTimeout);
    private DateTimeOffset _connected = DateTimeOffset.UtcNow;
    private string? _userAgent;

    /// <inheritdoc cref="IHubController.Id" />
    public override Guid Id => CurrentHubId;
    
    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var identity = User.GetOpenShockHubIdentity();
        CurrentHubId = identity.GetClaimValueAsGuid(OpenShockAuthClaims.HubId);
        CurrentHubOwnerId = identity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <summary>
    /// Base for hub websocket controllers
    /// </summary>
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    protected HubControllerBase(
        ISerializer<TIn> incomingSerializer,
        ISerializer<TOut> outgoingSerializer,
        HubLifetimeManager hubLifetimeManager,
        IServiceProvider serviceProvider,
        LcgOptions options,
        ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger
        ) : base(logger, incomingSerializer, outgoingSerializer)
    {
        _hubLifetimeManager = hubLifetimeManager;
        ServiceProvider = serviceProvider;
        _options = options;
        _keepAliveTimeoutTimer.Elapsed += async (_, _) =>
        {
            try
            {
                Logger.LogInformation("Keep alive timeout reached, closing websocket connection");
                await ForceClose(WebSocketCloseStatus.ProtocolError, "Keep alive timeout reached");
                WebSocket?.Abort();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while closing websocket connection from keep alive timeout");
            }
        };
        _keepAliveTimeoutTimer.Start();
    }


    private SemVersion? _firmwareVersion;

    /// <inheritdoc />
    protected override async Task<SuccessOrProblem> ConnectionPrecondition()
    {
        _connected = DateTimeOffset.UtcNow;

        if (HttpContext.Request.Headers.TryGetValue("Firmware-Version", out var header) &&
            SemVersion.TryParse(header, out var version))
        {
            _firmwareVersion = version;
        }
        else
        {
            return WebsocketError.WebsocketHubFirmwareVersionInvalid;
        }
        
        _userAgent = HttpContext.Request.Headers.UserAgent.ToString().Truncate(256);
        var hubLifetimeResult = await _hubLifetimeManager.TryAddDeviceConnection(5, this, LinkedToken);

        switch (hubLifetimeResult)
        {
            case HubLifetimeManager.Busy:
                Logger.LogWarning("Hub lifetime busy, closing connection");
                return WebsocketError.WebsocketHubLifetimeBusy;
            case Error:
                Logger.LogError("Hub lifetime error, closing connection");
                return ExceptionError.Exception;
            case LifetimeManager.HubLifetime hubLifetime:
                HubLifetime = hubLifetime;
                break;
        }

        return new Success();
    }
    
    private bool _unregistered;
    
    /// <summary>
    /// When the connection is destroyed
    /// </summary>
    protected override async Task UnregisterConnection()
    {
        if (_unregistered)
            return;
        _unregistered = true;
        
        await _hubLifetimeManager.RemoveDeviceConnection(this);
    }

    /// <inheritdoc />
    [NonAction]
    public abstract ValueTask Control(IList<ShockerCommand> controlCommands);

    /// <inheritdoc />
    [NonAction]
    public abstract ValueTask CaptivePortal(bool enable);

    /// <inheritdoc />
    [NonAction]
    public abstract ValueTask<bool> EmergencyStop();

    /// <inheritdoc />
    [NonAction]
    public abstract ValueTask OtaInstall(SemVersion version);

    /// <inheritdoc />
    [NonAction]
    public abstract ValueTask<bool> Reboot();

    /// <inheritdoc />
    [NonAction]
    public async Task DisconnectOld()
    {
        if (WebSocket is null)
            return;

        await ForceClose(WebSocketCloseStatus.NormalClosure, "Hub is connecting from a different location");
    }

    private static DateTimeOffset? GetBootedAtFromUptimeMs(ulong uptimeMs)
    {
        const ulong hundredYears = 100UL * 365UL * 24UL * 60UL * 60UL * 1000UL;
        if (uptimeMs > hundredYears) return null; // Sure, dude...
        
        var uptimeMsFp = (double)uptimeMs;
        
        var maxUptimeMs = ApiHardLimits.FirmwareMaxUptime.TotalMilliseconds;
        if (uptimeMsFp > maxUptimeMs) return null; // Yeah, ok bro.

        return DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(uptimeMsFp));
    }
    
    /// <summary>
    /// Keep the hub online
    /// </summary>
    protected async Task<bool> SelfOnline(ulong uptimeMs, ushort? latency = null, int? rssi = null)
    {
        var bootedAt = GetBootedAtFromUptimeMs(uptimeMs);
        if (!bootedAt.HasValue)
        {
            Logger.LogDebug("Client attempted to abuse reported boot time, uptime indicated that hub [{HubId}] booted prior to 2024", CurrentHubId);
            return false;
        }
        
        Logger.LogDebug("Received keep alive from hub [{HubId}]", CurrentHubId);

        // Reset the keep alive timeout
        _keepAliveTimeoutTimer.Interval = Duration.DeviceKeepAliveTimeout.TotalMilliseconds;

        await HubLifetime.Online(CurrentHubId, new SelfOnlineData()
        {
            Owner = CurrentHubOwnerId,
            Gateway = _options.GetPublicNodeId(),
            FirmwareVersion = _firmwareVersion!,
            ConnectedAt = _connected,
            UserAgent = _userAgent,
            BootedAt = bootedAt.Value,
            LatencyMs = latency,
            Rssi = rssi
        });

        return true;
    }
    
    /// <inheritdoc />
    protected override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}