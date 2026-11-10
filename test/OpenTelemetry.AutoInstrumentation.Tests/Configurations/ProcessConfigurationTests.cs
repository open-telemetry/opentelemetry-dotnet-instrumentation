// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.AutoInstrumentation.Tests.Util;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class ProcessConfigurationTests
{
    [Theory]
    [InlineData(null, (int)ProcessContextExtractionMode.Parent)]
    [InlineData("", (int)ProcessContextExtractionMode.Parent)]
    [InlineData("parent", (int)ProcessContextExtractionMode.Parent)]
    [InlineData("link", (int)ProcessContextExtractionMode.Link)]
    [InlineData("none", (int)ProcessContextExtractionMode.None)]
    public void LoadEnvVar_ExtractionMode(string? value, int expected)
    {
        var settings = LoadEnvironmentSettings(new NameValueCollection
        {
            { ConfigurationKeys.Traces.InstrumentationOptions.ProcessContextExtractionMode, value },
        });

        Assert.Equal((ProcessContextExtractionMode)expected, settings.InstrumentationOptions.ProcessContextExtractionMode);
        Assert.True(settings.InstrumentationOptions.ProcessContextInjectionEnabled);
        Assert.Contains(TracerInstrumentation.Process, settings.EnabledInstrumentations);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("1")]
    [InlineData("off")]
    public void LoadEnvVar_InvalidExtractionModeUsesParent(string value)
    {
        var settings = LoadEnvironmentSettings(new NameValueCollection
        {
            { ConfigurationKeys.Traces.InstrumentationOptions.ProcessContextExtractionMode, value },
        });

        Assert.Equal(ProcessContextExtractionMode.Parent, settings.InstrumentationOptions.ProcessContextExtractionMode);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("1")]
    [InlineData("off")]
    public void LoadEnvVar_InvalidExtractionModeFailsFast(string value)
    {
        var values = new NameValueCollection
        {
            { ConfigurationKeys.Traces.InstrumentationOptions.ProcessContextExtractionMode, value },
        };

        Assert.Throws<NotSupportedException>(() => LoadEnvironmentSettings(values, failFast: true));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("unsupported", true)]
    public void LoadEnvVar_InjectionEnabled(string? value, bool expected)
    {
        var settings = LoadEnvironmentSettings(new NameValueCollection
        {
            { ConfigurationKeys.Traces.InstrumentationOptions.ProcessContextInjectionEnabled, value },
        });

        Assert.Equal(expected, settings.InstrumentationOptions.ProcessContextInjectionEnabled);
        Assert.Equal(ProcessContextExtractionMode.Parent, settings.InstrumentationOptions.ProcessContextExtractionMode);
    }

    [Fact]
    public void LoadEnvVar_InvalidInjectionFlagFailsFast()
    {
        var values = new NameValueCollection
        {
            { ConfigurationKeys.Traces.InstrumentationOptions.ProcessContextInjectionEnabled, "unsupported" },
        };

        Assert.Throws<FormatException>(() => LoadEnvironmentSettings(values, failFast: true));
    }

    [Theory]
    [InlineData(null, null, null, true)]
    [InlineData("false", null, null, false)]
    [InlineData("false", "true", null, true)]
    [InlineData("true", "false", null, false)]
    [InlineData("false", "false", "true", true)]
    [InlineData("true", "true", "false", false)]
    public void LoadEnvVar_ProcessFlagInheritsAndOverridesGlobalFlags(string? all, string? traces, string? process, bool expected)
    {
        var settings = LoadEnvironmentSettings(new NameValueCollection
        {
            { ConfigurationKeys.InstrumentationEnabled, all },
            { ConfigurationKeys.Traces.TracesInstrumentationEnabled, traces },
            { "OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_ENABLED", process },
        });

        Assert.Equal(expected, settings.EnabledInstrumentations.Contains(TracerInstrumentation.Process));
    }

    [Fact]
    public void LoadFile_OmittedProcessDisablesInstrumentation()
    {
        var settings = LoadYamlSettings("{}");

        Assert.DoesNotContain(TracerInstrumentation.Process, settings.EnabledInstrumentations);
        Assert.Equal(ProcessContextExtractionMode.Parent, settings.InstrumentationOptions.ProcessContextExtractionMode);
        Assert.True(settings.InstrumentationOptions.ProcessContextInjectionEnabled);
    }

    [Theory]
    [InlineData("process:")]
    [InlineData("process: {}")]
    [InlineData("process: {context_extraction:}")]
    [InlineData("process: {context_extraction: {}}")]
    [InlineData("process: {context_injection:}")]
    [InlineData("process: {context_injection: {}}")]
    [InlineData("process: {context_extraction: null, context_injection: null}")]
    public void LoadFile_EmptyBlocksEnableDefaults(string traces)
    {
        var settings = LoadYamlSettings(traces);

        Assert.Contains(TracerInstrumentation.Process, settings.EnabledInstrumentations);
        Assert.Equal(ProcessContextExtractionMode.Parent, settings.InstrumentationOptions.ProcessContextExtractionMode);
        Assert.True(settings.InstrumentationOptions.ProcessContextInjectionEnabled);
    }

    [Theory]
    [InlineData("parent", true, (int)ProcessContextExtractionMode.Parent)]
    [InlineData("link", true, (int)ProcessContextExtractionMode.Link)]
    [InlineData("none", true, (int)ProcessContextExtractionMode.None)]
    [InlineData("parent", false, (int)ProcessContextExtractionMode.Parent)]
    public void LoadFile_DirectionOptions(string mode, bool injectionEnabled, int expected)
    {
        var enabled = injectionEnabled ? "true" : "false";
        var settings = LoadYamlSettings($"process: {{context_extraction: {{mode: {mode}}}, context_injection: {{enabled: {enabled}}}}}");

        Assert.Contains(TracerInstrumentation.Process, settings.EnabledInstrumentations);
        Assert.Equal((ProcessContextExtractionMode)expected, settings.InstrumentationOptions.ProcessContextExtractionMode);
        Assert.Equal(injectionEnabled, settings.InstrumentationOptions.ProcessContextInjectionEnabled);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("1")]
    [InlineData("off")]
    public void LoadFile_InvalidExtractionModeUsesParent(string mode)
    {
        var settings = LoadYamlSettings($"process: {{context_extraction: {{mode: {mode}}}}}");

        Assert.Equal(ProcessContextExtractionMode.Parent, settings.InstrumentationOptions.ProcessContextExtractionMode);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("off")]
    public void LoadFile_InvalidExtractionModeFailsFast(string mode)
    {
        Assert.Throws<NotSupportedException>(() => LoadYamlSettings($"process: {{context_extraction: {{mode: {mode}}}}}", failFast: true));
    }

    [Fact]
    public void LoadFile_OptionsSupportEnvironmentSubstitution()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_TEST_PROCESS_EXTRACTION_MODE", "link" },
            { "OTEL_TEST_PROCESS_INJECTION_ENABLED", "false" },
        });
        var settings = LoadYamlSettings("process: {context_extraction: {mode: '${OTEL_TEST_PROCESS_EXTRACTION_MODE}'}, context_injection: {enabled: '${OTEL_TEST_PROCESS_INJECTION_ENABLED}'}}");

        Assert.Equal(ProcessContextExtractionMode.Link, settings.InstrumentationOptions.ProcessContextExtractionMode);
        Assert.False(settings.InstrumentationOptions.ProcessContextInjectionEnabled);
    }

    private static TracerSettings LoadEnvironmentSettings(NameValueCollection values, bool failFast = false)
    {
        var settings = new TracerSettings();
        settings.LoadEnvVar(new Configuration(failFast, new NameValueConfigurationSource(failFast, values)));
        return settings;
    }

    private static TracerSettings LoadYamlSettings(string traces, bool failFast = false)
    {
        var configuration = Parser.ParseYamlContent<YamlConfiguration>($"instrumentation/development:\n  dotnet:\n    traces:\n      {traces}");
        configuration.FailFast = failFast;
        var settings = new TracerSettings();
        settings.LoadFile(configuration);
        return settings;
    }
}
