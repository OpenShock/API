using System.Runtime.InteropServices;

namespace OpenShock.Common.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetValueOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key,
        TValue defaultValue) where TKey : notnull
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out var exists);
        if (exists)
        {
            return value!;
        }
        
        value = defaultValue;
        return value;
    }
}