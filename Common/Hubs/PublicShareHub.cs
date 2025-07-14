using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Hubs;

public sealed class PublicShareHub : Hub<IPublicShareHub>
{
    private readonly ISessionService _sessionService;
    private readonly OpenShockContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;
    private readonly ILogger<PublicShareHub> _logger;
    private readonly IRedisPubService _redisPubService;
    private readonly IUserReferenceService _userReferenceService;
    private IReadOnlyList<PermissionType>? _tokenPermissions = null;

    public PublicShareHub(OpenShockContext db, IHubContext<UserHub, IUserHub> userHub, ILogger<PublicShareHub> logger,
        ISessionService sessionService, IRedisPubService redisPubService, IUserReferenceService userReferenceService)
    {
        _db = db;
        _userHub = userHub;
        _logger = logger;
        _redisPubService = redisPubService;
        _userReferenceService = userReferenceService;
        _sessionService = sessionService;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext?.GetRouteValue("Id") is not string param || !Guid.TryParse(param, out var id))
        {
            _logger.LogDebug("Aborting connection... id not found");
            Context.Abort();
            return;
        }
        
        BasicUserInfo? user = null;

        if (httpContext.TryGetUserSessionToken(out var sessionToken))
        {
            user = await SessionAuth(sessionToken);
            if (user is null)
            {
                _logger.LogDebug("Connection tried authentication with invalid user session cookie, terminating connection...");
                Context.Abort();
                return;
            }
        }
        
        _tokenPermissions = _userReferenceService.AuthReference is not { IsT1: true } ? null : _userReferenceService.AuthReference.Value.AsT1.Permissions;

        var exists = await _db.PublicShares.AnyAsync(x => x.Id == id && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow));
        if (!exists)
        {
            _logger.LogDebug("Aborting connection... public share not found");
            Context.Abort();
            return;
        }
        
        // TODO: Add token auth

        var customName = httpContext.Request.Query["name"].FirstOrDefault();

        if (user is null && customName is null)
        {
            _logger.LogDebug("customName was not set nor was the user authenticated, terminating connection...");
            Context.Abort();
            return;
        }

        if (customName is { Length: > HardLimits.UsernameMaxLength })
        {
            _logger.LogDebug("Custom name is too long, terminating connection...");
            Context.Abort();
            return;
        }

        var additionalItems = new Dictionary<string, object>
        {
            [ControlLogAdditionalItem.PublicShareId] = id
        };

        Context.Items[PublicShareCustomData] = new CustomDataHolder
        {
            PublicShareId = id,
            CustomName = customName,
            User = user,
            CachedControlLogSender = user is null
                ? new ControlLogSender
                {
                    Id = Guid.Empty,
                    Name = "Guest",
                    Image = GravatarUtils.GuestImageUrl,
                    ConnectionId = Context.ConnectionId,
                    CustomName = customName,
                    AdditionalItems = additionalItems
                }
                : new ControlLogSender
                {
                    Id = user.Id,
                    Image = user.Image,
                    Name = user.Name,
                    ConnectionId = Context.ConnectionId,
                    CustomName = null,
                    AdditionalItems = additionalItems
                }
        };
        await Groups.AddToGroupAsync(Context.ConnectionId, $"share-link-{param}");
        await Clients.Caller.Welcome(user is null ? AuthType.Guest : AuthType.Authenticated);
    }

    public Task Control(IReadOnlyList<Models.WebSocket.User.Control> shocks)
    {
        if (!_tokenPermissions.IsAllowedAllowOnNull(PermissionType.Shockers_Use)) return Task.CompletedTask;
        
        return ControlLogic.ControlPublicShare(shocks, _db, CustomData.CachedControlLogSender, _userHub.Clients,
            CustomData.PublicShareId, _redisPubService);
    }

    private CustomDataHolder CustomData => (CustomDataHolder)Context.Items[PublicShareCustomData]!;
    private const string PublicShareCustomData = "ShareLinkCustomData";

    private async Task<BasicUserInfo?> SessionAuth(string sessionToken)
    {
        var session = await _sessionService.GetSessionByToken(sessionToken);
        if (session is null) return null;
        
        return await _db.Users.Select(x => new BasicUserInfo
        {
            Id = x.Id,
            Image = x.GetImageUrl(),
            Name = x.Name
        }).FirstAsync(user => user.Id == session.UserId);
    }

    private sealed class CustomDataHolder
    {
        public required Guid PublicShareId { get; init; }
        public required BasicUserInfo? User { get; set; }
        public required string? CustomName { get; init; }
        public required ControlLogSender CachedControlLogSender { get; set; }
    }
    
    public enum AuthType
    {
        Authenticated,
        Guest
    }
}