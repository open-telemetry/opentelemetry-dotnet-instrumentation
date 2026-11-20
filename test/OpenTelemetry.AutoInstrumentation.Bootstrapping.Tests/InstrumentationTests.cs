// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Vendors.YamlDotNet.Core;

namespace OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests;

/// <summary>
/// When you add a new tests or change the name of one of the existing ones please remember to reflect the changes
/// in the build project, by updating the list in RunBootstrappingTests method of Build.Steps.cs.
/// Take notice that each test should be executed as a separate process. Because of that, the tests require
/// BOOSTRAPPING_TESTS environmental variable to be set, to mitigate the risk of running all of the tests at once.
/// </summary>
public sealed class InstrumentationTests : IDisposable
{
    private const string IncompatibleYaml = "file_format:\n  - '1.0'\n";

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

    [FactRequiringEnvVar]
    public void Initialize_WithIncompatibleFileConfigurationShape_DisablesInstrumentation()
    {
        var configFile = ConfigureFileBasedConfiguration(IncompatibleYaml);

        try
        {
            Instrumentation.Initialize();
            AssertInstrumentationDisabled();

            Instrumentation.Initialize();
            AssertInstrumentationDisabled();
        }
        finally
        {
            File.Delete(configFile);
        }
    }

    [FactRequiringEnvVar]
    public void Initialize_WithIncompatibleFileConfigurationShapeAndFailFast_Throws()
    {
        var configFile = ConfigureFileBasedConfiguration(IncompatibleYaml);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");

        try
        {
            Assert.Throws<YamlException>(Instrumentation.Initialize);
            AssertInstrumentationDisabled();

            // The failed configuration is terminal for this instrumentation lifetime.
            Instrumentation.Initialize();
            AssertInstrumentationDisabled();
        }
        finally
        {
            File.Delete(configFile);
        }
    }

    [FactRequiringEnvVar]
    public void Initialize_WhenAppDomainIsDisabled_DoesNotReparseFileConfiguration()
    {
        var configFile = ConfigureFileBasedConfiguration(IncompatibleYaml);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");
        InstrumentationBootstrapState.Disable();

        try
        {
            Instrumentation.Initialize();

            AssertInstrumentationDisabled();
        }
        finally
        {
            File.Delete(configFile);
        }
    }

    [FactRequiringEnvVar]
    public void Shutdown_WhenInstrumentationIsDisabled_DisposesCreatedLoggerProvider()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_LOG4NET_BRIDGE", "true");
        Environment.SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "none");
        Instrumentation.Initialize();

        using var provider = Instrumentation.LoggerProvider;
        Assert.NotNull(provider);
        using var processor = new DisposalTrackingLogProcessor();
        provider.AddProcessor(processor);

        InstrumentationBootstrapState.Disable();
        Assert.Null(Instrumentation.LoggerProvider);
        var onExit = typeof(Instrumentation).GetMethod("OnExit", BindingFlags.NonPublic | BindingFlags.Static)!;
        onExit.Invoke(null, [null, EventArgs.Empty]);

        Assert.True(processor.IsDisposed);
    }

    public void Dispose()
    {
        _otelActivitySource.Dispose();
        _customActivitySource.Dispose();
    }

    private static string ConfigureFileBasedConfiguration(string? contents = null)
    {
        var configFile = Path.GetTempFileName();
        if (contents is not null)
        {
            File.WriteAllText(configFile, contents);
        }

        Environment.SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_CONFIG_FILE", configFile);
        return configFile;
    }

    private void AssertInstrumentationDisabled()
    {
        Assert.True(Instrumentation.IsDisabled);
        Assert.Null(Instrumentation.PluginManager);
        Assert.Null(Instrumentation.LoggerProvider);
        Assert.Null(_otelActivitySource.StartActivity("OtelActivity"));
    }

    private sealed class DisposalTrackingLogProcessor : BaseProcessor<LogRecord>
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
