using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Authentication.Services;
using OpenShock.ServicesCommon.DeviceControl;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.ServicesCommon.Hubs;

public sealed class ShareLinkHub : Hub<IShareLinkHub>
{
    private readonly IRedisCollection<LoginSession> _userSessions;
    private readonly OpenShockContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;
    private readonly ILogger<ShareLinkHub> _logger;
    private readonly IRedisPubService _redisPubService;
    private readonly ITokenReferenceService<ApiToken> _tokenReferenceService;
    private IReadOnlyCollection<PermissionType>? _tokenPermissions = null;

    public ShareLinkHub(OpenShockContext db, IHubContext<UserHub, IUserHub> userHub, ILogger<ShareLinkHub> logger,
        IRedisConnectionProvider provider, IRedisPubService redisPubService, ITokenReferenceService<ApiToken> tokenReferenceService)
    {
        _db = db;
        _userHub = userHub;
        _logger = logger;
        _redisPubService = redisPubService;
        _tokenReferenceService = tokenReferenceService;
        _userSessions = provider.RedisCollection<LoginSession>(false);
    }

    public override async Task OnConnectedAsync()
    {
        _tokenPermissions = _tokenReferenceService.Token?.Permissions;
        
        var httpContext = Context.GetHttpContext();
        if (httpContext?.GetRouteValue("Id") is not string param || !Guid.TryParse(param, out var id))
        {
            _logger.LogWarning("Aborting connection... id not found");
            Context.Abort();
            return;
        }

        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.Id == id && (x.ExpiresOn == null || x.ExpiresOn > DateTime.UtcNow));
        if (!exists)
        {
            _logger.LogWarning("Aborting connection... share link not found");
            Context.Abort();
            return;
        }

        GenericIni? user = null;
        
        if (httpContext.Request.Cookies.TryGetValue("openShockSession", out var accessKeyCookie) &&
            !string.IsNullOrEmpty(accessKeyCookie))
        {
            user = await SessionAuth(accessKeyCookie);
        }
        
        if (httpContext.Request.Headers.TryGetValue("OpenShockSession", out var sessionKeyHeader) &&
            !string.IsNullOrEmpty(sessionKeyHeader))
        {
            user = await SessionAuth(sessionKeyHeader!);
        }
        
        // TODO: Add token auth

        var customName = httpContext.Request.Query["name"].FirstOrDefault();

        if (user == null && customName == null)
        {
            _logger.LogWarning("customName was not set nor was the user authenticated, terminating connection...");
            Context.Abort();
        }

        var additionalItems = new Dictionary<string, object>
        {
            [ControlLogAdditionalItem.ShareLinkId] = id
        };

        Context.Items[ShareLinkCustomData] = new CustomDataHolder
        {
            ShareLinkId = id,
            CustomName = customName,
            User = user,
            CachedControlLogSender = user == null
                ? new ControlLogSender
                {
                    Id = Guid.Empty,
                    Name = "Guest",
                    Image = new Uri("https://www.gravatar.com/avatar/0?d=https%3A%2F%2Fshocklink.net%2Fstatic%2Fimages%2FIcon512.png"),
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
        await Clients.Caller.Welcome(user != null ? AuthType.Authenticated : AuthType.Guest);
    }

    public Task Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks)
    {
        if (!_tokenPermissions.IsAllowedAllowOnNull(PermissionType.Shockers_Use)) return Task.CompletedTask;
        
        return ControlLogic.ControlShareLink(shocks, _db, CustomData.CachedControlLogSender, _userHub.Clients,
            CustomData.ShareLinkId, _redisPubService);
    }

    private CustomDataHolder CustomData => (CustomDataHolder)Context.Items[ShareLinkCustomData]!;
    private const string ShareLinkCustomData = "ShareLinkCustomData";

    private async Task<GenericIni?> SessionAuth(string sessionKey)
    {
        var session = await _userSessions.FindByIdAsync(sessionKey);
        if (session == null) return null;
        return await _db.Users.Select(x => new GenericIni
        {
            Id = x.Id,
            Image = GravatarUtils.GetImageUrl(x.Email),
            Name = x.Name
        }).FirstAsync(user => user.Id == session.UserId);
    }

    private sealed class CustomDataHolder
    {
        public required Guid ShareLinkId { get; init; }
        public required GenericIni? User { get; set; }
        public required string? CustomName { get; init; }
        public required ControlLogSender CachedControlLogSender { get; set; }
    }
    
    public enum AuthType
    {
        Authenticated,
        Guest
    }
}