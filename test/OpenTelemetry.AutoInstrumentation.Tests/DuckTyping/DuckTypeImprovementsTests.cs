// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Tests.DuckTyping;

public class DuckTypeImprovementsTests
{
    private interface IFirstClass
    {
        ISecondClass? Value { get; }
    }

    private interface ISecondClass
    {
        string? Name { get; }
    }

    [Fact]
    public void CopiesNullableDuckCopyValues()
    {
        var source = new FirstClass();

        Assert.Null(source.DuckCast<FirstCopy>().Value);

        source.Value = new SecondClass { Name = "value" };

        var copy = source.DuckCast<FirstCopy>();
        Assert.True(copy.Value.HasValue);
        Assert.Equal("value", copy.Value.Value.Name);
    }

    [Fact]
#pragma warning disable CA2263 // Exercise the non-generic extension overloads.
    public void NullInstancesDoNotThrowFromTryAndAsExtensions()
    {
        object? instance = null;

        Assert.False(instance.TryDuckCast<IFirstClass>(out var genericValue));
        Assert.Null(genericValue);
        Assert.False(instance.TryDuckCast(typeof(IFirstClass), out var value));
        Assert.Null(value);
        Assert.Null(instance.DuckAs<IFirstClass>());
        Assert.Null(instance.DuckAs(typeof(IFirstClass)));
        Assert.False(instance.DuckIs<IFirstClass>());
        Assert.False(instance.DuckIs(typeof(IFirstClass)));
        Assert.False(instance.TryDuckImplement(typeof(IFirstClass), out var implementation));
        Assert.Null(implementation);
    }
#pragma warning restore CA2263

    [Fact]
    public void RejectsPropertyOnlyDuckCopyStruct()
    {
        var source = new SecondClass { Name = "value" };

        Assert.Throws<DuckTypeDuckCopyStructDoesNotContainsAnyField>(() => source.DuckCast<PropertyOnlyCopy>());
    }

    [DuckCopy]
    private struct FirstCopy
    {
        public SecondCopy? Value;

        public FirstCopy()
        {
            Value = null;
        }
    }

    [DuckCopy]
    private struct SecondCopy
    {
        public string? Name;

        public SecondCopy()
        {
            Name = null;
        }
    }

    [DuckCopy]
    private struct PropertyOnlyCopy
    {
        public string? Name { get; set; }
    }

    private sealed class FirstClass
    {
        public SecondClass? Value { get; set; }
    }

    private sealed class SecondClass
    {
        public string? Name { get; set; }
    }
}
