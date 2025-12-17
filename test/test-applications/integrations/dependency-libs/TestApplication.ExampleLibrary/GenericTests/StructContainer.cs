// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.GenericTests;

public struct StructContainer<T>
{
    public List<T> Items { get; }

    public long Id { get; }

    public StructContainer(long id, List<T> items)
    {
        Id = id;
        Items = items;
    }
}
