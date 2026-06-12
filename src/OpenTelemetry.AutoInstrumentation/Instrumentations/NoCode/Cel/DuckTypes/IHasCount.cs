// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel.DuckTypes;

/// <summary>
/// Duck type interface for collections with Count property.
/// Covers: List&lt;T&gt;, HashSet&lt;T&gt;, Dictionary&lt;K,V&gt;, Queue&lt;T&gt;, Stack&lt;T&gt;, LinkedList&lt;T&gt;,
/// ArrayList, Hashtable, ImmutableList&lt;T&gt;, IReadOnlyCollection&lt;T&gt;, etc.
/// </summary>
internal interface IHasCount
{
    int Count { get; }
}
