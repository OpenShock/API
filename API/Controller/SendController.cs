using Microsoft.AspNetCore.Mvc;
using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Controller;

[ApiController]
[Route("send")]
public class SendController
{
    [HttpPost]
    public async Task<bool> Send(Data data)
    {
        await WebSocketController.Instance.QueueMessage(new BaseResponse
        {
            ResponseType = ResponseType.Control,
            Data = new List<ControlResponse>
            {
                new()
                {
                    Id = 3068,
                    Type = ControlType.Vibrate,
                    Intensity = 25,
                    Duration = 5000
                },
                new()
                {
                    Id = 3045,
                    Type = ControlType.Vibrate,
                    Intensity = 50,
                    Duration = 2500
                }
            }
        });
        return true;
    }
    
    public class Data
    {
        public required string Text { get; set; }
    }
}