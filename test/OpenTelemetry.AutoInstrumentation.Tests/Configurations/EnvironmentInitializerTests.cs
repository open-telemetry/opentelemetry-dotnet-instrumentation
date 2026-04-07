// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class EnvironmentInitializerTests
{
    private const string OtelVariableName = "OTEL_SETTING";
    private const string NonOtelVariableName = "OTHER_SETTING";
    private const string SomeValue = "val";

    [Fact]
    public void SetsOTelEnvironmentVariable_WhenItWasEmpty()
    {
        using var envScope = new EnvironmentScope(new()
        {
            { OtelVariableName, null },
        });

        EnvironmentInitializer.Initialize(new NameValueCollection
        {
            { OtelVariableName, SomeValue }
        });

        var actual = Environment.GetEnvironmentVariable(OtelVariableName);
        Assert.Equal(SomeValue, actual);
    }

    [Fact]
    public void Noop_WhenEnvironmentVariableWasAlreadySet()
    {
        using var envScope = new EnvironmentScope(new()
        {
            { OtelVariableName, SomeValue },
        });

        EnvironmentInitializer.Initialize(new NameValueCollection
        {
            { OtelVariableName, "different" }
        });

        var actual = Environment.GetEnvironmentVariable(OtelVariableName);
        Assert.Equal(SomeValue, actual);
    }

    [Fact]
    public void Noop_WhenSettingIsNonOTel()
    {
        using var envScope = new EnvironmentScope(new()
        {
            { NonOtelVariableName, null },
        });

        EnvironmentInitializer.Initialize(new NameValueCollection
        {
            { NonOtelVariableName, SomeValue }
        });

        var actual = Environment.GetEnvironmentVariable(NonOtelVariableName);
        Assert.True(string.IsNullOrEmpty(actual), "initializer should ignore variables non starting from OTEL_");
    }
}
