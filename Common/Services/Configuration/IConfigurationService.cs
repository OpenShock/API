using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services.Configuration;

public interface IConfigurationService
{
    IQueryable<ConfigurationItem> GetAllItemsQuery();

    Task<Union4<Success, AlreadyExists, InvalidNameFormat, InvalidValueFormat>> TryAddItemAsync(string name, string description, ConfigurationValueType type, string value);
    Task<Union4<Success, NotFound, InvalidNameFormat, InvalidValueFormat>> TryUpdateItemAsync(string name, string? description, string? value);
    Task<Union3<Success, NotFound, InvalidNameFormat>> TryDeleteItemAsync(string name);

    Task<ConfigGetResult<string>> TryGetStringAsync(string name);
    Task<bool> TrySetStringAsync(string name, string value);

    Task<ConfigGetResult<bool>> TryGetBoolAsync(string name);
    Task<bool> TrySetBoolAsync(string name, bool value);

    Task<ConfigGetResult<int>> TryGetIntAsync(string name);
    Task<bool> TrySetIntAsync(string name, int value);

    Task<ConfigGetResult<float>> TryGetFloatAsync(string name);
    Task<bool> TrySetFloatAsync(string name, float value);

    Task<ConfigGetResult<T>> TryGetJsonAsync<T>(string name);
    Task<bool> TrySetJsonAsync<T>(string name, T value);
}

public enum ConfigGetOutcome : byte
{
    Value,
    NotFound,
    InvalidValueType,
    InvalidValueFormat
}

/// <summary>
/// Result of reading a typed configuration value: the value, or why it couldn't be read.
/// Stores the payload directly (tag + typed field) instead of boxing through <c>object?</c>
/// </summary>
public readonly record struct ConfigGetResult<T>
{
    public ConfigGetOutcome Outcome { get; }
    public T Value { get; }

    private ConfigGetResult(ConfigGetOutcome outcome)
    {
        Outcome = outcome;
        Value = default!;
    }

    public ConfigGetResult(T value)
    {
        Outcome = ConfigGetOutcome.Value;
        Value = value;
    }

    public static implicit operator ConfigGetResult<T>(T value) => new(value);

    public static ConfigGetResult<T> NotFound() => new(ConfigGetOutcome.NotFound);
    public static ConfigGetResult<T> InvalidValueType() => new(ConfigGetOutcome.InvalidValueType);
    public static ConfigGetResult<T> InvalidValueFormat() => new(ConfigGetOutcome.InvalidValueFormat);
}

public sealed class AlreadyExists;
public sealed class InvalidNameFormat;
public sealed class InvalidValueType;
public sealed class InvalidValueFormat;