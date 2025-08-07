using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using OneOf;
using OneOf.Types;
using OpenShock.Common.OpenShockDb;
using System.Buffers;
using System.Globalization;
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

    public IQueryable<ConfigurationItem> GetAllItemsQuery()
    {
        return _db.Configuration.AsNoTracking();
    }

    private static readonly SearchValues<char> AllowedChars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ_");
    private static bool IsValidName(ReadOnlySpan<char> name)
    {
        return !name.IsEmpty && !name.ContainsAnyExcept(AllowedChars);
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    private static bool IsValidValueFormat(ConfigurationValueType type, string value)
    {
        return type switch
        {
            ConfigurationValueType.String => value is not null,
            ConfigurationValueType.Bool => bool.TryParse(value, out _),
            ConfigurationValueType.Int => int.TryParse(value, CultureInfo.InvariantCulture, out _),
            ConfigurationValueType.Float => float.TryParse(value, CultureInfo.InvariantCulture, out float f) && (float.IsNormal(f) || f == 0f),
            ConfigurationValueType.Json => IsValidJson(value),
            _ => false
        };
    }

    public async Task<OneOf<Success, AlreadyExists, InvalidNameFormat, InvalidValueFormat>> TryAddItemAsync(string name, string description, ConfigurationValueType type, string value)
    {
        // Validate name (only uppercase letters and underscores)
        if (!IsValidName(name))
        {
            return new InvalidNameFormat();
        }

        // Validate value format against the expected type
        if (!IsValidValueFormat(type, value))
        {
            return new InvalidValueFormat();
        }

        // Create new configuration entry
        var now = DateTime.UtcNow;
        var item = new ConfigurationItem
        {
            Name = name,
            Description = description,
            Type = type,
            Value = value,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            _db.Configuration.Add(item);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) // Unique constaint violation
        {
            return new AlreadyExists();
        }

        // Cache the new type/value pair for fast retrieval
        await _cache.SetAsync(name, new TypeValuePair(type, value));

        return new Success();
    }

    public async Task<OneOf<Success, NotFound, InvalidNameFormat, InvalidValueFormat, InvalidValueType>> TryUpdateItemAsync(string name, string? description, string? value)
    {
        // Validate name
        if (!IsValidName(name))
            return new InvalidNameFormat();

        // If nothing to update, exit early
        if (description is null && value is null)
            return new Success();

        // Load existing item
        var item = await _db.Configuration.FirstOrDefaultAsync(ci => ci.Name == name);
        if (item is null)
            return new NotFound();

        // If updating the value, validate it against the stored type
        if (value is not null)
        {
            if (!IsValidValueFormat(item.Type, value))
                return new InvalidValueFormat();

            item.Value = value;
        }

        // If updating the description, apply it
        if (description is not null)
        {
            item.Description = description;
        }

        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Refresh cache if value changed
        if (value is not null)
        {
            await _cache.SetAsync(
                name.ToUpperInvariant(),
                new TypeValuePair(item.Type, item.Value)
            );
        }

        return new Success();
    }

    public async Task<OneOf<Success, NotFound, InvalidNameFormat>> TryDeleteItemAsync(string name)
    {
        // Find the item
        var item = await _db.Configuration.FirstOrDefaultAsync(ci => ci.Name == name);
        if (item is null)
            return new NotFound();

        // Remove and persist
        _db.Configuration.Remove(item);
        await _db.SaveChangesAsync();

        // Evict from cache
        await _cache.RemoveAsync(name);

        return new Success();
    }

    private sealed record TypeValuePair(ConfigurationValueType Type, string Value);
    private async Task<TypeValuePair?> TryGetTypeValuePair(string name)
    {
        name = name.ToUpperInvariant();
        return await _cache.GetOrCreateAsync(name, async cancellationToken =>
        {
            var pair = await GetAllItemsQuery()
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
            if (!IsValidName(name))
            {
                _logger.LogError("system tried to set an invalid configuration name: {name}", name);
                return false;
            }

            var now = DateTime.UtcNow;

            item = new ConfigurationItem
            {
                Name = name,
                Description = "Auto-added by ConfigurationService",
                Type = type,
                Value = newValue,
                UpdatedAt = now,
                CreatedAt = now
            };

            _db.Configuration.Add(item);
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
        await _cache.SetAsync(name, new TypeValuePair(type, newValue));
        return true;
    }

    public async Task<OneOf<string, NotFound, InvalidValueType>> TryGetStringAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.String) return new InvalidValueType();
        return pair.Value;
    }

    public Task<bool> TrySetStringAsync(string name, string value) =>
        SetValueAsync(name, value, ConfigurationValueType.String);

    public async Task<OneOf<bool, NotFound, InvalidValueType, InvalidValueFormat>> TryGetBoolAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Bool) return new InvalidValueType();
        if (!bool.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse bool for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidValueFormat();
        }
        return value;
    }

    public Task<bool> TrySetBoolAsync(string name, bool value) =>
        SetValueAsync(name, value.ToString(), ConfigurationValueType.Bool);

    public async Task<OneOf<int, NotFound, InvalidValueType, InvalidValueFormat>> TryGetIntAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Int) return new InvalidValueType();
        if (!int.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse int for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidValueFormat();
        }
        return value;
    }

    public Task<bool> TrySetIntAsync(string name, int value) =>
        SetValueAsync(name, value.ToString(), ConfigurationValueType.Int);

    public async Task<OneOf<float, NotFound, InvalidValueType, InvalidValueFormat>> TryGetFloatAsync(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Float) return new InvalidValueType();
        if (!float.TryParse(pair.Value, out var value))
        {
            _logger.LogWarning("Failed to parse float for '{Name}': Value='{Value}'", name, pair.Value);
            return new InvalidValueFormat();
        }
        return value;
    }

    public Task<bool> TrySetFloatAsync(string name, float value) =>
        SetValueAsync(name, value.ToString("R"), ConfigurationValueType.Float);

    public async Task<OneOf<T, NotFound, InvalidValueType, InvalidValueFormat>> TryGetJsonAsync<T>(string name)
    {
        var pair = await TryGetTypeValuePair(name);
        if (pair is null) return new NotFound();
        if (pair.Type != ConfigurationValueType.Json) return new InvalidValueType();

        try
        {
            var obj = JsonSerializer.Deserialize<T>(pair.Value);
            return obj is not null ? obj : new InvalidValueFormat();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON for '{Name}'", name);
            return new InvalidValueFormat();
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