using System.Text.Json.Serialization;
using OpenShock.Internal.Common.JsonSerialization;
using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "login-session-v2")]
public sealed class LoginSession
{
    [RedisIdField] [Indexed(IndexEmptyAndMissing = false)] public required string Id { get; init; }
    [Indexed(IndexEmptyAndMissing = false)] public required Guid UserId { get; init; }
    [Indexed(IndexEmptyAndMissing = false)] public required string Ip { get; init; }
    [Indexed(IndexEmptyAndMissing = false)] public required string UserAgent { get; init; }
    [Indexed(IndexEmptyAndMissing = false)] public Guid? PublicId { get; init; }
    [JsonConverter(typeof(UnixMillisecondsDateTimeOffsetConverter))]
    public DateTimeOffset? Created { get; init; }
    [JsonConverter(typeof(UnixMillisecondsDateTimeOffsetConverter))]
    public DateTimeOffset? Expires { get; set; }
    [JsonConverter(typeof(UnixMillisecondsDateTimeOffsetConverter))]
    public DateTimeOffset? LastUsed { get; set; }
    public string? AsnOrg { get; init; }
    public bool? IsVpn { get; init; }
    public string? CountryCode { get; init; }
    public string? City { get; init; }
}