// <copyright file="InstrumentationTests.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests;

/// <summary>
/// When you add a new tests or change the name of one of the existing ones please remember to reflect the changes
/// in the build project, by updating the list in RunBootstrappingTests method of Build.Steps.cs.
/// Take notice that each test should be executed as a separate process. Because of that, the tests require
/// BOOSTRAPPING_TESTS environmental variable to be set, to mitigate the risk of running all of the tests at once.
/// </summary>
public class InstrumentationTests
{
    private readonly ActivitySource _otelActivitySource = new("OpenTelemetry.AutoInstrumentation.*");
    private readonly ActivitySource _customActivitySource = new("Custom");

    [FactRequiringEnvVar]
    public void Initialize_WithDisabledFlag_DoesNotCreateTracerProvider()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOAD_AT_STARTUP", "false");

        Instrumentation.Initialize();
        var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
        var customActivity = _customActivitySource.StartActivity("CustomActivity");

        Assert.Null(otelActivity);
        Assert.Null(customActivity);
    }

    [FactRequiringEnvVar]
    public void Initialize_WithDefaultFlag_CreatesTracerProvider()
    {
        Instrumentation.Initialize();

        Instrumentation.Initialize();
        var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
        var customActivity = _customActivitySource.StartActivity("CustomActivity");

        Assert.NotNull(otelActivity);
        Assert.Null(customActivity);
    }

    [FactRequiringEnvVar]
    public void Initialize_WithEnabledFlag_CreatesTracerProvider()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOAD_AT_STARTUP", "true");

        Instrumentation.Initialize();
        var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
        var customActivity = _customActivitySource.StartActivity("CustomActivity");

        Assert.NotNull(otelActivity);
        Assert.Null(customActivity);
    }

    [FactRequiringEnvVar]
    public void Initialize_WithPreviouslyCreatedTracerProvider_WorksCorrectly()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOAD_AT_STARTUP", "false");
        var tracer = Sdk
            .CreateTracerProviderBuilder()
            .AddSource("Custom")
            .Build();

        Instrumentation.Initialize();
        var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
        var customActivity = _customActivitySource.StartActivity("CustomActivity");

        Assert.Null(otelActivity);
        Assert.NotNull(customActivity);
    }

    public sealed class FactRequiringEnvVarAttribute : FactAttribute
    {
        private const string EnvVar = "BOOSTRAPPING_TESTS";

        public FactRequiringEnvVarAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvVar)))
            {
                Skip = $"Ignore as {EnvVar} is not set";
            }
        }
    }
}
