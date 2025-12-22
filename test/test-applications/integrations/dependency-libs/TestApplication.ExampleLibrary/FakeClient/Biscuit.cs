// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.FakeClient;

public class Biscuit<T> : Biscuit
{
    public T? Reward { get; set; }
}

public class Biscuit
{
    public Guid Id { get; set; }

    public string? Message { get; set; }

#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    public List<object?> Treats { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists

#pragma warning disable CA1034 // Nested types should not be visible
    public class Cookie
#pragma warning restore CA1034 // Nested types should not be visible
    {
        public bool IsYummy { get; set; }

#pragma warning disable CA1034 // Nested types should not be visible
        public class Raisin
#pragma warning restore CA1034 // Nested types should not be visible
        {
            public bool IsPurple { get; set; }
        }
    }
}

#pragma warning disable CA1815 // Override equals and operator equals on value types
public struct StructBiscuit
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    public Guid Id { get; set; }

#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible
    public struct Cookie
#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public bool IsYummy { get; set; }
    }
}
