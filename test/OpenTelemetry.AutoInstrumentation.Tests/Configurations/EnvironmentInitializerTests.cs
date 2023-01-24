// <copyright file="EnvironmentInitializerTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
