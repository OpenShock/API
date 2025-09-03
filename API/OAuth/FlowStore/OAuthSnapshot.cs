using Redis.OM.Modeling;

namespace OpenShock.API.OAuth.FlowStore;

[Document(StorageType = StorageType.Json, IndexName = IndexName)]
public sealed class OAuthSnapshot
{
    public const string IndexName = "oauth-snapshot";
    
    [RedisField] public required string FlowId { get; init; }
    public required string Provider { get; init; }
    public required string ExternalId { get; init; }
    public required string? Email { get; init; }
    public required string? DisplayName { get; init; }
    public required DateTimeOffset IssuedUtc { get; init; }
}