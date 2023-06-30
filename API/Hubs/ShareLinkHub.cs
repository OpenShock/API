using Microsoft.AspNetCore.SignalR;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Hubs;

public class ShareLinkHub : Hub<IShareLinkHub>
{
    private readonly ShockLinkContext _db;

    public ShareLinkHub(ShockLinkContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var param = Context.GetHttpContext()?.GetRouteValue("Id") as string;
        if (param == null || !Guid.TryParse(param, out var id))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"share-link-{param}");
    }
    
    public async Task Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks)
    {
        var additionalItems = new Dictionary<string, object>();
        var apiTokenId =  Context.User?.FindFirst(ControlLogAdditionalItem.ApiTokenId);
        if(apiTokenId != null) additionalItems[ControlLogAdditionalItem.ApiTokenId] = apiTokenId.Value;

        /*var sender = await _db.Users.Where(x => x.Id == UserId).Select(x => new ControlLogSender
        {
            Id = x.Id,
            Name = x.Name,
            Image = ImagesApi.GetImageRoot(x.Image),
            ConnectionId = Context.ConnectionId,
            AdditionalItems = additionalItems
        }).SingleAsync();
        
        await ControlLogic.Control(shocks, _db, sender, Clients);*/
    }
}