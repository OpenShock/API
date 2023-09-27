using Newtonsoft.Json;

namespace OpenShock.API.Models;

public class CloudflareImagePost
{
    public required IEnumerable<string> Messages { get; set; }
    public required IEnumerable<string> Errors { get; set; }
    public required bool Success { get; set; }
    [JsonProperty("result_info")] public string? ResultInfo { get; set; }
    public required ResultPost Result { get; set; }

    public class ResultPost
    {
        public required Guid Id { get; set; }
        public required string Filename { get; set; }
        public required DateTime Uploaded { get; set; }
        public required bool RequireSignedUrls { get; set; }
        public required IEnumerable<string> Variants { get; set; }
    }
}