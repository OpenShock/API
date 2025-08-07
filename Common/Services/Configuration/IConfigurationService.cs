using OneOf;
using OneOf.Types;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.Configuration;

public interface IConfigurationService
{
    IQueryable<ConfigurationItem> GetAllItemsQuery();

    Task<OneOf<Success, AlreadyExists, InvalidNameFormat, InvalidValueFormat>> TryAddItemAsync(string name, string description, ConfigurationValueType type, string value);
    Task<OneOf<Success, NotFound, InvalidNameFormat, InvalidValueFormat, InvalidValueType>> TryUpdateItemAsync(string name, string? description, string? value);
    Task<OneOf<Success, NotFound, InvalidNameFormat>> TryDeleteItemAsync(string name);

    Task<OneOf<string, NotFound, InvalidValueType>> TryGetStringAsync(string name);
    Task<bool> TrySetStringAsync(string name, string value);

    Task<OneOf<bool, NotFound, InvalidValueType, InvalidValueFormat>> TryGetBoolAsync(string name);
    Task<bool> TrySetBoolAsync(string name, bool value);

    Task<OneOf<int, NotFound, InvalidValueType, InvalidValueFormat>> TryGetIntAsync(string name);
    Task<bool> TrySetIntAsync(string name, int value);

    Task<OneOf<float, NotFound, InvalidValueType, InvalidValueFormat>> TryGetFloatAsync(string name);
    Task<bool> TrySetFloatAsync(string name, float value);

    Task<OneOf<T, NotFound, InvalidValueType, InvalidValueFormat>> TryGetJsonAsync<T>(string name);
    Task<bool> TrySetJsonAsync<T>(string name, T value);
}

public readonly struct AlreadyExists;
public readonly struct InvalidNameFormat;
public readonly struct InvalidValueType;
public readonly struct InvalidValueFormat;