﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Common.JsonSerialization;

public sealed class FlagCompatibleJsonStringEnumConverter : JsonConverterFactory
{
    private static readonly JsonStringEnumConverter JsonStringEnumConverter = new();

    public override bool CanConvert(Type typeToConvert) =>
        !typeToConvert.IsDefined(typeof(FlagsAttribute), false) &&
        JsonStringEnumConverter.CanConvert(typeToConvert);
    
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        JsonStringEnumConverter.CreateConverter(typeToConvert, options);
}