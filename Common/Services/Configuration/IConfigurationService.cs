using OneOf;
using OneOf.Types;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.Configuration;

public interface IConfigurationService
{
    Task<ConfigurationItem[]> GetAllItemsAsync(string name);

    Task<OneOf<ConfigurationItem, NotFound>> GetItemAsync(string name);
    Task<bool> CheckItemExistsAsync(string name);
    Task<OneOf<ConfigurationValueType, NotFound>> TryGetItemTypeAsync(string name);

    Task<OneOf<string, NotFound, InvalidType>> TryGetStringAsync(string name);
    Task<bool> TrySetStringAsync(string name, string value);

    Task<OneOf<bool, NotFound, InvalidType, InvalidConfiguration>> TryGetBoolAsync(string name);
    Task<bool> TrySetBoolAsync(string name, bool value);

    Task<OneOf<int, NotFound, InvalidType, InvalidConfiguration>> TryGetIntAsync(string name);
    Task<bool> TrySetIntAsync(string name, int value);

    Task<OneOf<float, NotFound, InvalidType, InvalidConfiguration>> TryGetFloatAsync(string name);
    Task<bool> TrySetFloatAsync(string name, float value);

    Task<OneOf<T, NotFound, InvalidType, InvalidConfiguration>> TryGetJsonAsync<T>(string name);
    Task<bool> TrySetJsonAsync<T>(string name, T value);
}

public readonly struct InvalidType;
public readonly struct InvalidConfiguration;