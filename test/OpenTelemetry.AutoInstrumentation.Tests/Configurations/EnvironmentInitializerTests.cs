// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class EnvironmentInitializerTests
{
    private const string OtelResourceAttributesVariableName = "OTEL_RESOURCE_ATTRIBUTES";
    private const string OtelServiceVariableName = "OTEL_SERVICE_NAME";
    private const string OtelVariableName = "OTEL_SETTING";
    private const string NonOtelVariableName = "OTHER_SETTING";
    private const string SomeValue = "val";

    [Fact]
    public void SetsOTelEnvironmentVariable_WhenItWasEmpty()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
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

    [Theory]
    [InlineData(OtelServiceVariableName)]
    [InlineData(OtelResourceAttributesVariableName)]
    public void Noop_WhenSettingResources(string variableName)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, null);
            EnvironmentInitializer.Initialize(new NameValueCollection
            {
                { variableName, SomeValue }
            });
            var actual = Environment.GetEnvironmentVariable(variableName);

            Assert.True(string.IsNullOrEmpty(actual), $"initializer should ignore {variableName} resource variable");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
