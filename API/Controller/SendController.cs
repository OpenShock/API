using Microsoft.AspNetCore.Mvc;
using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Controller;

[ApiController]
[Route("send")]
public class SendController
{
    [HttpGet]
    public async Task<bool> Send([FromQuery] ControlType controlType, [FromQuery] byte intensity,
        [FromQuery] uint duration, [FromQuery] int device)
    {
        duration = Math.Min(duration, 30000);
        var lel = new List<ControlResponse>();

        switch (device)
        {
            case 1:
                lel.Add(new()
                {
                    Id = 3068,
                    Type = controlType,
                    Intensity = intensity,
                    Duration = duration
                });
                break;
            case 2: 
                lel.Add(new()
                {
                    Id = 3045,
                    Type = controlType,
                    Intensity = intensity,
                    Duration = duration
                });
                break;
            case 0:
                lel.Add(new()
                {
                    Id = 3045,
                    Type = controlType,
                    Intensity = intensity,
                    Duration = duration
                });
                lel.Add(new()
                {
                    Id = 3068,
                    Type = controlType,
                    Intensity = intensity,
                    Duration = duration
                });
                break;
        }

        await WebSocketController.Instance.QueueMessage(new BaseResponse
        {
            ResponseType = ResponseType.Control,
            Data = lel
        });
        return true;
    }
}