// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Tests.DuckTyping;

public class ValueWithTypeTests
{
    private interface IPropertyProxy
    {
        ValueWithType<string> Property { get; set; }
    }

    private interface IFieldProxy
    {
        [DuckField]
        ValueWithType<string> Field { get; set; }
    }

    private interface IMethodProxy
    {
        ValueWithType<string> Read();
    }

    private interface IChildProxy
    {
        string Name { get; }
    }

    private interface IChainedProxy
    {
        ValueWithType<IChildProxy> Child { get; set; }
    }

    private interface IChainedFieldProxy
    {
        [DuckField]
        ValueWithType<IChildProxy> ChildField { get; set; }
    }

    [Fact]
    public void KeepsDeclaredTypeForNullPropertiesFieldsAndMethods()
    {
        var target = new Target();

        var property = target.DuckCast<IPropertyProxy>();
        var field = target.DuckCast<IFieldProxy>();
        var method = target.DuckCast<IMethodProxy>();

        Assert.Null(property.Property.Value);
        Assert.Equal(typeof(string), property.Property.Type);
        Assert.Null(field.Field.Value);
        Assert.Equal(typeof(string), field.Field.Type);
        Assert.Null(method.Read().Value);
        Assert.Equal(typeof(string), method.Read().Type);

        property.Property = ValueWithType<string>.Create("property", typeof(string));
        field.Field = ValueWithType<string>.Create("field", typeof(string));

        Assert.Equal("property", target.Property);
        Assert.Equal("field", target.Field);
        Assert.Equal("property", method.Read().Value);
    }

    [Fact]
    public void KeepsDeclaredTypeWhenDuckChainingNullAndNonNullValues()
    {
        var target = new Target();
        var proxy = target.DuckCast<IChainedProxy>();

        Assert.Null(proxy.Child.Value);
        Assert.Equal(typeof(Child), proxy.Child.Type);

        target.Child = new Child { Name = "child" };

        Assert.Equal("child", proxy.Child.Value?.Name);
        Assert.Equal(typeof(Child), proxy.Child.Type);

        var replacement = new Child { Name = "replacement" };
        proxy.Child = ValueWithType<IChildProxy>.Create(replacement.DuckCast<IChildProxy>(), typeof(Child));
        Assert.Same(replacement, target.Child);

        var fieldProxy = target.DuckCast<IChainedFieldProxy>();
        fieldProxy.ChildField = ValueWithType<IChildProxy>.Create(replacement.DuckCast<IChildProxy>(), typeof(Child));
        Assert.Same(replacement, target.ChildField);
    }

    private sealed class Target
    {
#pragma warning disable SA1401 // The field is the duck typing target.
        public string? Field;

        public Child? ChildField;
#pragma warning restore SA1401

        public Target()
        {
            Field = null;
            ChildField = null;
        }

        public string? Property { get; set; }

        public Child? Child { get; set; }

        public string? Read() => Property;
    }

    private sealed class Child
    {
        public string Name { get; set; } = string.Empty;
    }
}
