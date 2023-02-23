using Newtonsoft.Json.Linq;
using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Models.WebSocket;

public class BaseRequest
{
    public required RequestType RequestType { get; set; }
    public JToken? Data { get; set; }
}