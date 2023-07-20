using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Authentication;
using ShockLink.API.DeviceControl;
using ShockLink.API.Utils;
using ShockLink.Common;
using ShockLink.Common.Models;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Hubs;

public sealed class ShareLinkHub : Hub<IShareLinkHub>
{
    private readonly IRedisCollection<LoginSession> _userSessions;
    private readonly ShockLinkContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;
    private readonly ILogger<ShareLinkHub> _logger;

    public ShareLinkHub(ShockLinkContext db, IHubContext<UserHub, IUserHub> userHub, ILogger<ShareLinkHub> logger,
        IRedisConnectionProvider provider)
    {
        _db = db;
        _userHub = userHub;
        _logger = logger;
        _userSessions = provider.RedisCollection<LoginSession>(false);
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext?.GetRouteValue("Id") is not string param || !Guid.TryParse(param, out var id))
        {
            _logger.LogWarning("Aborting connection...");
            Context.Abort();
            return;
        }

        GenericIni? user = null;

        if (httpContext.Request.Headers.TryGetValue("ShockLinkSession", out var sessionKeyHeader) &&
            !string.IsNullOrEmpty(sessionKeyHeader))
        {
            user = await SessionAuth(sessionKeyHeader!);
        }

        if (httpContext.Request.Cookies.TryGetValue("shockLinkSession", out var accessKeyCookie) &&
            !string.IsNullOrEmpty(accessKeyCookie))
        {
            user = await SessionAuth(accessKeyCookie);
        }

        var customName = httpContext.Request.Query["name"].FirstOrDefault();

        if (user == null && customName == null)
        {
            _logger.LogWarning("customName was not set nor was the user authenticated, terminating connection...");
            Context.Abort();
        }

        var additionalItems = new Dictionary<string, object>
        {
            [ControlLogAdditionalItem.ShareLinkId] = CustomData.ShareLinkId
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
                    Image = ImagesApi.GetImageRoot(Constants.DefaultAvatar),
                    ConnectionId = Context.ConnectionId,
                    CustomName = CustomData.CustomName,
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
    }

    public Task Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks)
    {
        return ControlLogic.ControlShareLink(shocks, _db, CustomData.CachedControlLogSender, _userHub.Clients,
            CustomData.ShareLinkId);
    }

    private CustomDataHolder CustomData => (CustomDataHolder)Context.Items[ShareLinkCustomData]!;
    private const string ShareLinkCustomData = "ShareLinkCustomData";

    private async Task<GenericIni?> SessionAuth(string sessionKey)
    {
        var session = await _userSessions.FindByIdAsync(sessionKey);
        if (session == null) return null;
        return await _db.Users.Select(x => new GenericIni()
        {
            Id = x.Id,
            Image = ImagesApi.GetImageRoot(x.Image),
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
}