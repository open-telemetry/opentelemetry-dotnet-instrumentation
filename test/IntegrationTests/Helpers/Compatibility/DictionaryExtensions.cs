// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Collections;
using System.Collections.Concurrent;

namespace IntegrationTests.Helpers.Compatibility;

internal static class DictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        return dictionary.TryGetValue(key, out var value)
                    ? value
                    : default;
    }

    public static TValue? GetValueOrDefault<TValue>(this IDictionary dictionary, object key)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        return dictionary.TryGetValue(key, out TValue? value)
                    ? value
                    : default;
    }

    public static bool TryGetValue<TValue>(this IDictionary dictionary, object key, out TValue? value)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        object? valueObj;

        try
        {
            // per its contract, IDictionary.Item[] should return null instead of throwing an exception
            // if a key is not found, but let's use try/catch to be defensive against misbehaving implementations
            valueObj = dictionary[key];
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            valueObj = null;
        }

        switch (valueObj)
        {
            case TValue valueTyped:
                value = valueTyped;
                return true;
            case IConvertible convertible:
                value = (TValue)convertible.ToType(typeof(TValue), provider: null);
                return true;
            default:
                value = default;
                return false;
        }
    }

    public static bool IsEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            return concurrentDictionary.IsEmpty;
        }

        return dictionary.Count == 0;
    }
}
#endif
