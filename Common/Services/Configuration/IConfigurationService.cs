using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services.Configuration;

public interface IConfigurationService
{
    IQueryable<ConfigurationItem> GetAllItemsQuery();

    Task<Union4<Success, AlreadyExists, InvalidNameFormat, InvalidValueFormat>> TryAddItemAsync(string name, string description, ConfigurationValueType type, string value, CancellationToken cancellationToken = default);
    Task<Union4<Success, NotFound, InvalidNameFormat, InvalidValueFormat>> TryUpdateItemAsync(string name, string? description, string? value, CancellationToken cancellationToken = default);
    Task<Union3<Success, NotFound, InvalidNameFormat>> TryDeleteItemAsync(string name, CancellationToken cancellationToken = default);

    Task<ConfigGetResult<string>> TryGetStringAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> TrySetStringAsync(string name, string value, CancellationToken cancellationToken = default);

    Task<ConfigGetResult<bool>> TryGetBoolAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> TrySetBoolAsync(string name, bool value, CancellationToken cancellationToken = default);

    Task<ConfigGetResult<int>> TryGetIntAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> TrySetIntAsync(string name, int value, CancellationToken cancellationToken = default);

    Task<ConfigGetResult<float>> TryGetFloatAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> TrySetFloatAsync(string name, float value, CancellationToken cancellationToken = default);

    Task<ConfigGetResult<T>> TryGetJsonAsync<T>(string name, CancellationToken cancellationToken = default);
    Task<bool> TrySetJsonAsync<T>(string name, T value, CancellationToken cancellationToken = default);
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