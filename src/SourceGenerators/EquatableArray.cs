// <copyright file="EquatableArray.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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

    private ReadOnlySpan<T> AsSpan()
    {
        return _array.AsSpan();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }
}
