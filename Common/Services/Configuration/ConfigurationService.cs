using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using OneOf;
using OneOf.Types;
using OpenShock.Common.OpenShockDb;
using System.Text.Json;

namespace OpenShock.Common.Services.Configuration;

public sealed class ConfigurationService : IConfigurationService
{
    private readonly HybridCache _cache;
    private readonly OpenShockContext _db;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(HybridCache cache, OpenShockContext db, ILogger<ConfigurationService> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    private sealed record TypeValuePair(ConfigurationValueType Type, string Value);
    private async Task<TypeValuePair?> TryGetTypeValuePair(string name)
    {
        return await _cache.GetOrCreateAsync(name, async cancellationToken =>
        {
            var pair = await _db.Configuration
                .Where(ci => ci.Name == name)
                .Select(ci => new TypeValuePair(ci.Type, ci.Value))
                .FirstOrDefaultAsync(cancellationToken);

            return pair;
        });
    }

    private async Task<bool> SetValueAsync(string name, string newValue, ConfigurationValueType type)
    {
        var item = await _db.Configuration.FirstOrDefaultAsync(c => c.Name == name);

        if (item is null)
        {
            var now = DateTime.UtcNow;

            _db.Configuration.Add(new ConfigurationItem
            {
                Name = name,
                Description = "Auto-added by ConfigurationService",
                Type = type,
                Value = newValue,
                UpdatedAt = now,
                CreatedAt = now
            });
        }
        else
        {
            if (item.Type != type)
            {
                _logger.LogWarning("Type mismatch for config '{Name}': expected {Expected}, got {Actual}", name, item.Type, type);
                return false;
            }

            item.Value = newValue;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync(name); // Invalidate hybrid cache
        return true;
    }

    public async Task<ConfigurationItem[]> GetAllItemsAsync(string name)
    {
        return await _db.Configuration.ToArrayAsync();
    }

    public async Task<OneOf<ConfigurationItem, NotFound>> GetItemAsync(string name)
    {
        var item = await _db.Configuration.FirstOrDefaultAsync(ci => ci.Name == name);
        return item is null ? new NotFound() : item;
    }

    public async Task<bool> CheckItemExistsAsync(string name)
    {
        return await _db.Configuration.AnyAsync(ci => ci.Name == name);
    }

    public async Task<OneOf<ConfigurationValueType, NotFound>> TryGetItemTypeAsync(string name)
    {
        var type = await _db.Configuration
            .Where(c => c.Name == name)
            .Select(c => (ConfigurationValueType?)c.Type)
            .FirstOrDefaultAsync();

        return type.HasValue ? type.Value : new NotFound();
    }

    public async Task<OneOf<string, NotFound, InvalidType>> TryGetStringAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.String) return new InvalidType();
        return pair.Value;
    }

    public Task<bool> TrySetStringAsync(string name, string value) =>
        SetValueAsync(name, value, ConfigurationValueType.String);

    public async Task<OneOf<bool, NotFound, InvalidType, InvalidConfiguration>> TryGetBoolAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Bool) return new InvalidType();
        if (!bool.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse bool for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidConfiguration();
        }
        return value;
    }

    public Task<bool> TrySetBoolAsync(string name, bool value) =>
        SetValueAsync(name, value.ToString(), ConfigurationValueType.Bool);

    public async Task<OneOf<int, NotFound, InvalidType, InvalidConfiguration>> TryGetIntAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Int) return new InvalidType();
        if (!sbyte.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse sbyte for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidConfiguration();
        }
        return value;
    }

    public Task<bool> TrySetIntAsync(string name, int value) =>
        SetValueAsync(name, value.ToString(), ConfigurationValueType.Int);

    public async Task<OneOf<float, NotFound, InvalidType, InvalidConfiguration>> TryGetFloatAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Float) return new InvalidType();
        if (!float.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse float for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidConfiguration();
        }
        return value;
    }

    public Task<bool> TrySetFloatAsync(string name, float value) =>
        SetValueAsync(name, value.ToString("R"), ConfigurationValueType.Float);

    public async Task<OneOf<T, NotFound, InvalidType, InvalidConfiguration>> TryGetJsonAsync<T>(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Json) return new InvalidType();

        try
        {
            var obj = JsonSerializer.Deserialize<T>(pair.Value);
            return obj is not null ? obj : new InvalidConfiguration();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON for '{Name}'", name);
            return new InvalidConfiguration();
        }
    }

    public async Task<bool> TrySetJsonAsync<T>(string name, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            return await SetValueAsync(name, json, ConfigurationValueType.Json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to serialize value for '{Name}'", name);
            return false;
        }
    }
}