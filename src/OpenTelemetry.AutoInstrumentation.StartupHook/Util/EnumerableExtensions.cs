// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NET6_0_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Provides DistinctBy for frameworks before .NET 6.
/// On .NET 6+ the built-in LINQ DistinctBy exists, so this is excluded.
/// Delete this file when the project targets .NET6+.
/// </summary>
internal static class EnumerableExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        var seen = new HashSet<TKey>(comparer);
        foreach (var element in source)
        {
            if (seen.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }
}

#endif
