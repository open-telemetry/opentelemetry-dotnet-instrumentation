// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;

namespace SourceGenerators;

internal readonly record struct EquatableArray<T>
    : IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[] _array;

    public EquatableArray(T[] array)
    {
        _array = array;
    }

    public int Count => _array.Length;

    public bool Equals(EquatableArray<T> array)
    {
        return _array.AsSpan().SequenceEqual(array.AsSpan());
    }

    public override int GetHashCode()
    {
        // based on https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
        // Overflow is fine, just wrap
        unchecked
        {
            var hash = (int)2166136261;

            foreach (var equatable in _array)
            {
                hash = (hash * 16777619) ^ equatable.GetHashCode();
            }

            return hash;
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    private ReadOnlySpan<T> AsSpan()
    {
        return _array.AsSpan();
    }
}
