﻿using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.Common.Models.WebSocket;

public sealed class DeviceOnlineState
{
    public required Guid Device { get; set; }
    public required bool Online { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion? FirmwareVersion { get; set; }
}