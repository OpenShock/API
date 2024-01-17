using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Semver;

namespace OpenShock.Common.Models.Services.Ota;

public sealed class OtaItem
{
    public required int Id { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required OtaUpdateStatus Status { get; init; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion Version { get; init; }
}