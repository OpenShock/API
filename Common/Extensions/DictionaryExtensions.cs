using System.Runtime.InteropServices;

namespace OpenShock.Common.Extensions;

public static class DictionaryExtensions
{
    public static void AppendValue<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) where TKey : notnull
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out _);
        if (list is null)
        {
            list = [value];
            return;
        }

        list.Add(value);
    }
}