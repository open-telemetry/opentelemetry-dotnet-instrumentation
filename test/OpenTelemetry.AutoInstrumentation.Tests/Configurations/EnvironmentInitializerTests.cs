// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
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
        try
        {
            Environment.SetEnvironmentVariable(OtelVariableName, null);

            EnvironmentInitializer.Initialize(new NameValueCollection
            {
                { OtelVariableName, SomeValue }
            });
            var actual = Environment.GetEnvironmentVariable(OtelVariableName);

            Assert.Equal(SomeValue, actual);
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtelVariableName, null);
        }
    }

    [Fact]
    public void Noop_WhenEnvironmentVariableWasAlreadySet()
    {
        try
        {
            Environment.SetEnvironmentVariable(OtelVariableName, SomeValue);

            EnvironmentInitializer.Initialize(new NameValueCollection
            {
                { OtelVariableName, "different" }
            });
            var actual = Environment.GetEnvironmentVariable(OtelVariableName);

            Assert.Equal(SomeValue, actual);
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtelVariableName, null);
        }
    }

    [Fact]
    public void Noop_WhenSettingIsNonOTel()
    {
        try
        {
            Environment.SetEnvironmentVariable(NonOtelVariableName, null);

            EnvironmentInitializer.Initialize(new NameValueCollection
            {
                { NonOtelVariableName, SomeValue }
            });
            var actual = Environment.GetEnvironmentVariable(NonOtelVariableName);

            Assert.True(string.IsNullOrEmpty(actual), "initializer should ignore variables non starting from OTEL_");
        }
        finally
        {
            Environment.SetEnvironmentVariable(NonOtelVariableName, null);
        }
    }
}
