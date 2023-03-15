using Newtonsoft.Json;

namespace ShockLink.API.Models;

public class CloudflareImagePost
{
    public IEnumerable<string> Messages { get; set; }
    public IEnumerable<string> Errors { get; set; }
    public bool Success { get; set; }
    [JsonProperty("result_info")] public string ResultInfo { get; set; }
    public ResultPost Result { get; set; }

    public class ResultPost
    {
        public Guid Id { get; set; }
        public string Filename { get; set; }
        public DateTime Uploaded { get; set; }
        public bool RequireSignedUrls { get; set; }
        public IEnumerable<string> Variants { get; set; }
    }
}