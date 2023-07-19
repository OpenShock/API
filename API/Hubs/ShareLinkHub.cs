using Microsoft.AspNetCore.SignalR;
using ShockLink.API.DeviceControl;
using ShockLink.API.Utils;
using ShockLink.Common;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Hubs;

public sealed class ShareLinkHub : Hub<IShareLinkHub>
{
    private readonly ShockLinkContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;
    private readonly ILogger<ShareLinkHub> _logger;

    public ShareLinkHub(ShockLinkContext db, IHubContext<UserHub, IUserHub> userHub, ILogger<ShareLinkHub> logger)
    {
        _db = db;
        _userHub = userHub;
        _logger = logger;
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

        Context.Items[ShareLinkCustomData] = new CustomDataHolder
        {
            ShareLinkId = id,
            CustomName = httpContext?.Request.Headers["Name"].ToString() ?? "Not name set"
        };
        await Groups.AddToGroupAsync(Context.ConnectionId, $"share-link-{param}");
    }

    public async Task Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks)
    {
        await ControlLogic.ControlShareLink(shocks, _db, new ControlLogSender
        {
            Id = Guid.Empty,
            Name = "Guest",
            Image = ImagesApi.GetImageRoot(Constants.DefaultAvatar),
            ConnectionId = Context.ConnectionId,
            CustomName = CustomData.CustomName,
            AdditionalItems = new Dictionary<string, object>
            {
                [ControlLogAdditionalItem.ShareLinkId] = CustomData.ShareLinkId
            }
        }, _userHub.Clients, CustomData.ShareLinkId);
    }

    private CustomDataHolder CustomData => (CustomDataHolder)Context.Items[ShareLinkCustomData]!;
    private const string ShareLinkCustomData = "ShareLinkCustomData";

    private sealed class CustomDataHolder
    {
        public required Guid ShareLinkId { get; init; }
        public required string CustomName { get; init; }
    }
}