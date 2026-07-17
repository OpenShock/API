using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services.Configuration;

public interface IConfigurationService
{
    IQueryable<ConfigurationItem> GetAllItemsQuery();

    Task<Union4<Success, AlreadyExists, InvalidNameFormat, InvalidValueFormat>> TryAddItemAsync(string name, string description, ConfigurationValueType type, string value);
    Task<Union4<Success, NotFound, InvalidNameFormat, InvalidValueFormat>> TryUpdateItemAsync(string name, string? description, string? value);
    Task<Union3<Success, NotFound, InvalidNameFormat>> TryDeleteItemAsync(string name);

    Task<Union3<string, NotFound, InvalidValueType>> TryGetStringAsync(string name);
    Task<bool> TrySetStringAsync(string name, string value);

    Task<Union4<bool, NotFound, InvalidValueType, InvalidValueFormat>> TryGetBoolAsync(string name);
    Task<bool> TrySetBoolAsync(string name, bool value);

    Task<Union4<int, NotFound, InvalidValueType, InvalidValueFormat>> TryGetIntAsync(string name);
    Task<bool> TrySetIntAsync(string name, int value);

    Task<Union4<float, NotFound, InvalidValueType, InvalidValueFormat>> TryGetFloatAsync(string name);
    Task<bool> TrySetFloatAsync(string name, float value);

    Task<Union4<T, NotFound, InvalidValueType, InvalidValueFormat>> TryGetJsonAsync<T>(string name);
    Task<bool> TrySetJsonAsync<T>(string name, T value);
}

public readonly struct AlreadyExists;
public readonly struct InvalidNameFormat;
public readonly struct InvalidValueType;
public readonly struct InvalidValueFormat;