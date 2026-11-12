// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class EnvironmentInitializerTests
{
    private const string OtelVariableName = ConfigurationKeys.FileBasedConfiguration.FileName;
    private const string ManagedVariableName = "OTEL_TRACES_EXPORTER";
    private const string NonOtelVariableName = "OTHER_SETTING";
    private const string SomeValue = "val";

    [Fact]
    public void SetsYamlSelectionEnvironmentVariable_WhenItWasEmpty()
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
    public void Noop_WhenYamlSelectionEnvironmentVariableWasAlreadySet()
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
    [InlineData("OTEL_SERVICE_NAME")]
    [InlineData("OTEL_RESOURCE_ATTRIBUTES")]
    [InlineData(ManagedVariableName)]
    public void Noop_WhenSettingIsManaged(string variableName)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { variableName, null }
        });

        EnvironmentInitializer.Initialize(new NameValueCollection
        {
            { variableName, SomeValue }
        });

        Assert.Null(Environment.GetEnvironmentVariable(variableName));
    }
}
