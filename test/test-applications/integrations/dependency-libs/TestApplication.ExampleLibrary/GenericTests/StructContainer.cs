// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.GenericTests;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public struct StructContainer<T>
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
#pragma warning disable CA1002 // Do not expose generic lists
    public List<T> Items { get; }
#pragma warning restore CA1002 // Do not expose generic lists

    public long Id { get; }

#pragma warning disable CA1002 // Do not expose generic lists
    public StructContainer(long id, List<T> items)
#pragma warning restore CA1002 // Do not expose generic lists
    {
        Id = id;
        Items = items;
    }
}
