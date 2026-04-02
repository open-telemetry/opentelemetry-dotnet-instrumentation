// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
public sealed class InstrumentationTests : IDisposable
{
    private readonly ActivitySource _otelActivitySource = new("OpenTelemetry.AutoInstrumentation.*");
    private readonly ActivitySource _customActivitySource = new("Custom");

    [FactRequiringEnvVar]
    public void Initialize_WithDisabledFlag_DoesNotCreateTracerProvider()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED", "false");

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
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED", "true");

        Instrumentation.Initialize();
        var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
        var customActivity = _customActivitySource.StartActivity("CustomActivity");

        Assert.NotNull(otelActivity);
        Assert.Null(customActivity);
    }

    [FactRequiringEnvVar]
    public void Initialize_WithPreviouslyCreatedTracerProvider_WorksCorrectly()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED", "false");
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

    public void Dispose()
    {
        _otelActivitySource.Dispose();
        _customActivitySource.Dispose();
    }
}
