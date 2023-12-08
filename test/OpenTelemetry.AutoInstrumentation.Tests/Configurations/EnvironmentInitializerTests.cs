// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using FluentAssertions;
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

            EnvironmentInitializer.Initialize(new NameValueCollection()
            {
                { OtelVariableName, SomeValue }
            });
            var actual = Environment.GetEnvironmentVariable(OtelVariableName);

            actual.Should().Be(SomeValue);
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

            EnvironmentInitializer.Initialize(new NameValueCollection()
            {
                { OtelVariableName, "different" }
            });
            var actual = Environment.GetEnvironmentVariable(OtelVariableName);

            actual.Should().Be(SomeValue);
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

            EnvironmentInitializer.Initialize(new NameValueCollection()
            {
                { NonOtelVariableName, SomeValue }
            });
            var actual = Environment.GetEnvironmentVariable(NonOtelVariableName);

            actual.Should().BeNullOrEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable(NonOtelVariableName, null);
        }
    }
}
