using Microsoft.AspNetCore.Mvc;
using ShockLink.API.RedisPubSub;
using ShockLink.Common.Models;
using ShockLink.Common.Models.WebSocket;
using ShockLink.Common.Redis.PubSub;

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

        await PubSubManager.SendControlMessage(new ControlMessage
        {
            Shocker = Guid.NewGuid(),
            ControlMessages = new List<ControlMessage.DeviceControlInfo>
            {
                new()
                {
                    DeviceId = Guid.Parse("adc73b37-716a-4f3b-ab38-70e2aef774c0"),
                    Shocks = new List<ControlMessage.DeviceControlInfo.ShockerControlInfo>()
                    {
                        new()
                        {
                            Id = Guid.Parse("41e4aecd-62f8-4c58-8035-34e651d720fb"),
                            Intensity = 50,
                            Duration = 2000,
                            Type = ControlType.Vibrate,
                            RfId = 3045
                        }
                    }
                }
            }
        });
        return true;
    }
}