using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "login-session")]
public sealed class LoginSession
{
    [RedisIdField] [Indexed] public required string Id { get; set; }
    [Indexed] public required Guid UserId { get; set; }
    [Indexed] public required string Ip { get; set; }
    [Indexed] public required string UserAgent { get; set; }
}