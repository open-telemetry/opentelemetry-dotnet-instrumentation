// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Tests.DuckTyping;

public class LongTypeNameTests
{
    private interface ILongNamedTypeProxy
    {
        string Value { get; }
    }

    [Fact]
    public void CanDuckTypeLongGenericName()
    {
        Type targetType = new LongNamedGenericTarget<string>().GetType();
        for (var i = 0; i < 20; i++)
        {
            targetType = typeof(LongNamedGenericTarget<>).MakeGenericType(targetType);
        }

        Assert.True(targetType.FullName!.Length > 1024);

        var instance = Activator.CreateInstance(targetType)!;
        var proxy = instance.DuckCast<ILongNamedTypeProxy>();

        Assert.Equal("It works!", proxy.Value);
    }

    private sealed class LongNamedGenericTarget<T>
    {
        public string Value { get; } = "It works!";
    }
}
