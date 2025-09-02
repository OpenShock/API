using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using OpenShock.Common.Utils;
using System.Text.Json;

namespace OpenShock.API.Utils;

public sealed class DistributedCacheSecureDataFormat<T> : ISecureDataFormat<T>
{
    private readonly RedisCache _redisCache;
    private readonly DistributedCacheEntryOptions _entryOptions;

    public DistributedCacheSecureDataFormat(string connectionString, TimeSpan secretLifeSpan)
    {
        _redisCache = new RedisCache(new RedisCacheOptions
        {
            Configuration = connectionString
        });
        _entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = secretLifeSpan
        };
    }
    public string Protect(T data)
    {
        string key = CryptoUtils.RandomString(32);
        _redisCache.Set(key, JsonSerializer.SerializeToUtf8Bytes(data), _entryOptions);
        return key;
    }

    public string Protect(T data, string? purpose)
    {
        return Protect(data);
    }

    public T? Unprotect(string? protectedText)
    {
        if (protectedText is null)
        {
            return default;
        }

        byte[]? bytes = _redisCache.Get(protectedText);
        if (bytes is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes);
    }

    public T? Unprotect(string? protectedText, string? purpose)
    {
        return Unprotect(protectedText);
    }
}