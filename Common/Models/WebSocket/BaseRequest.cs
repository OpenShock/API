using Newtonsoft.Json.Linq;

namespace ShockLink.Common.Models.WebSocket;

public class BaseRequest
{
    public required RequestType RequestType { get; set; }
    public JToken? Data { get; set; }
}