// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.SelfDiagnostics;
using OpenTelemetry.Trace;
using AgentLogLevel = OpenTelemetry.AutoInstrumentation.Logging.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace OpenTelemetry.AutoInstrumentation.Tests.Diagnostics;

[Collection("Non-Parallel Collection")]
public sealed class SelfDiagnosticsConfigurationTests : IDisposable
{
    private const int SdkDefaultFileSizeLimitKilobytes = 10_240;
    private const int SdkDefaultMaxRetainedFiles = 10;

    private static readonly string[] AllVariables =
    [
        "OTEL_LOG_LEVEL",
        "OTEL_DOTNET_AUTO_LOGGER",
        "OTEL_DOTNET_AUTO_LOG_DIRECTORY",
        "OTEL_DOTNET_AUTO_LOG_FILE_SIZE",
        "OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS",
        "OTEL_DOTNET_SELF_DIAGNOSTICS_LOG_DIRECTORY",
        "OTEL_SDK_DISABLED",
    ];

    public void Dispose()
    {
        OtelLogging.Reset();
    }

    [Fact]
    public void WhenNothingIsConfigured_Then_AgentDefaultsApply()
    {
        using var scope = CreateScope();
        var options = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(LogLevel.Information, options.MinimumLevel);
        Assert.Equal(OtelLogging.GetLogDirectory(), options.LogDirectory);
        Assert.False(options.LogToStdout, "Stdout should be disabled by default.");
        Assert.False(options.LogToStderr, "Stderr should be disabled by default.");
        Assert.Equal(SdkDefaultFileSizeLimitKilobytes, options.FileSizeLimitKilobytes);
        Assert.Equal(SdkDefaultMaxRetainedFiles, options.MaxRetainedFiles);
    }

    [Theory]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("warn", LogLevel.Warning)]
    public void WhenLogLevelIsSet_Then_SdkParsedLevelIsLeftAlone(string configuredValue, LogLevel sdkParsedLevel)
    {
        using var scope = CreateScope(("OTEL_LOG_LEVEL", configuredValue));
        var options = new SelfDiagnosticsOptions { MinimumLevel = sdkParsedLevel };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(sdkParsedLevel, options.MinimumLevel);
    }

    [Fact]
    public void WhenConsoleLoggerIsConfigured_Then_StdoutSinkOnly()
    {
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOGGER", "console"));
        var options = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.True(options.LogToStdout, "The console logger should enable stdout.");
        Assert.False(options.LogToStderr, "The console logger should not enable stderr.");
        Assert.Null(options.LogDirectory);
    }

    [Fact]
    public void WhenNoneLoggerIsConfigured_Then_NoSink()
    {
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOGGER", "none"));
        var options = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.False(options.LogToStdout, "The 'none' logger should disable stdout.");
        Assert.False(options.LogToStderr, "The 'none' logger should disable stderr.");
        Assert.Null(options.LogDirectory);
    }

    [Fact]
    public void WhenAgentLogDirectoryIsConfigured_Then_ItIsUsedAndCreated()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOG_DIRECTORY", directory));
        var options = new SelfDiagnosticsOptions();

        try
        {
            SelfDiagnosticsConfiguration.Configure(options, null);

            Assert.Equal(directory, options.LogDirectory);
            Assert.True(Directory.Exists(directory), "The configured agent log directory should be created.");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void WhenSdkSinksVariableIsSet_Then_SinkPropertiesAreLeftAlone()
    {
        using var scope = CreateScope(("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "stdout"));
        var options = new SelfDiagnosticsOptions { LogToStdout = true };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.True(options.LogToStdout, "Agent mapping should preserve the SDK stdout sink.");
        Assert.False(options.LogToStderr, "Agent mapping should not add stderr to the SDK sinks.");
        Assert.Null(options.LogDirectory);
    }

    [Fact]
    public void WhenSdkSinksAndLogDirectoryAreSet_Then_SdkDirectoryIsLeftAlone()
    {
        const string sdkDirectory = "sdk-directory";

        using var scope = CreateScope(
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "file"),
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_LOG_DIRECTORY", sdkDirectory));
        var options = new SelfDiagnosticsOptions { LogDirectory = sdkDirectory };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(sdkDirectory, options.LogDirectory);
    }

    [Fact]
    public void WhenOnlySdkLogDirectoryIsSet_Then_SdkDirectoryIsLeftAloneAndFileSinkApplies()
    {
        const string sdkDirectory = "sdk-directory";

        using var scope = CreateScope(("OTEL_DOTNET_SELF_DIAGNOSTICS_LOG_DIRECTORY", sdkDirectory));
        var options = new SelfDiagnosticsOptions { LogDirectory = sdkDirectory };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(sdkDirectory, options.LogDirectory);
        Assert.False(options.LogToStdout, "An SDK log directory alone should not enable stdout.");
        Assert.False(options.LogToStderr, "An SDK log directory alone should not enable stderr.");
    }

    [Theory]
    [InlineData("console", true)]
    [InlineData("none", false)]
    public void WhenAgentLoggerIsNotFileAndSdkLogDirectoryIsSet_Then_SdkDirectoryIsLeftAlone(string agentLogger, bool expectedStdout)
    {
        const string sdkDirectory = "sdk-directory";

        using var scope = CreateScope(
            ("OTEL_DOTNET_AUTO_LOGGER", agentLogger),
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_LOG_DIRECTORY", sdkDirectory));
        var options = new SelfDiagnosticsOptions { LogDirectory = sdkDirectory };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(sdkDirectory, options.LogDirectory);
        Assert.Equal(expectedStdout, options.LogToStdout);
        Assert.False(options.LogToStderr, "The agent logger should not enable stderr.");
    }

    [Theory]
    [InlineData("1500", 2)]
    [InlineData("1", 1)]
    [InlineData("1023", 1)]
    [InlineData("1024", 1)]
    [InlineData("1025", 2)]
    [InlineData("2199023254529", int.MaxValue)]
    [InlineData("9223372036854775807", int.MaxValue)]
    [InlineData("-1", SdkDefaultFileSizeLimitKilobytes)]
    public void WhenFileSizeIsConfigured_Then_ItIsMappedToKilobytes(string configuredValue, int expectedKilobytes)
    {
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", configuredValue));
        var options = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(expectedKilobytes, options.FileSizeLimitKilobytes);
    }

    [Fact]
    public void WhenFileSizeIsNotConfigured_Then_FileSizeIsLeftAlone()
    {
        using var scope = CreateScope();
        var options = new SelfDiagnosticsOptions { FileSizeLimitKilobytes = 7 };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(7, options.FileSizeLimitKilobytes);
    }

    [Fact]
    public void MaxRetainedFilesAndEnvironmentVariablesAreNeverTouched()
    {
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1500"), ("OTEL_DOTNET_AUTO_LOGGER", "console"));
        var options = new SelfDiagnosticsOptions
        {
            MaxRetainedFiles = 3,
            EnvironmentVariables = EnvironmentVariableLogMode.None,
        };

        SelfDiagnosticsConfiguration.Configure(options, null);

        Assert.Equal(3, options.MaxRetainedFiles);
        Assert.Equal(EnvironmentVariableLogMode.None, options.EnvironmentVariables);
    }

    [Fact]
    public void WhenPluginOverrides_Then_PluginWinsWhereItWrote()
    {
        using var scope = CreateScope();
        SelfDiagnosticsOverridingPlugin.CallCount = 0;

        var options = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(options, CreatePluginManager<SelfDiagnosticsOverridingPlugin>());

        Assert.Equal(LogLevel.Error, options.MinimumLevel);
        Assert.True(options.LogToStderr, "The plugin should enable stderr.");
        Assert.Equal(OtelLogging.GetLogDirectory(), options.LogDirectory);
        Assert.Equal(1, SelfDiagnosticsOverridingPlugin.CallCount);
    }

    [Fact]
    public void WhenPluginOverrides_Then_PluginBeatsSdkVariablesOnlyWhereItWrote()
    {
        using var scope = CreateScope(("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "stdout"), ("OTEL_LOG_LEVEL", "debug"));
        var options = new SelfDiagnosticsOptions { LogToStdout = true, MinimumLevel = LogLevel.Debug };

        SelfDiagnosticsConfiguration.Configure(options, CreatePluginManager<SelfDiagnosticsOverridingPlugin>());

        Assert.Equal(LogLevel.Error, options.MinimumLevel);
        Assert.True(options.LogToStderr, "The plugin should enable stderr.");
        Assert.True(options.LogToStdout, "The plugin should preserve the SDK stdout sink it does not override.");
    }

    [Fact]
    public void WhenPluginThrows_Then_AgentMappingStandsAndNothingEscapes()
    {
        using var scope = CreateScope();
        var options = new SelfDiagnosticsOptions();

        var exception = Record.Exception(() => SelfDiagnosticsConfiguration.Configure(options, CreatePluginManager<SelfDiagnosticsThrowingPlugin>()));

        Assert.Null(exception);
        Assert.Equal(LogLevel.Information, options.MinimumLevel);
        Assert.Equal(OtelLogging.GetLogDirectory(), options.LogDirectory);
        Assert.False(options.LogToStdout, "A plugin failure should leave stdout disabled.");
        Assert.False(options.LogToStderr, "A plugin failure should leave stderr disabled.");
    }

    [Fact]
    public void WhenNoPluginImplementsTheHook_Then_ResultEqualsNoPluginManager()
    {
        using var scope = CreateScope();
        var withEmptyManager = new SelfDiagnosticsOptions();
        var withoutManager = new SelfDiagnosticsOptions();

        SelfDiagnosticsConfiguration.Configure(withEmptyManager, new PluginManager(new PluginsSettings()));
        SelfDiagnosticsConfiguration.Configure(withoutManager, null);

        foreach (var options in new[] { withEmptyManager, withoutManager })
        {
            Assert.Equal(LogLevel.Information, options.MinimumLevel);
            Assert.Equal(OtelLogging.GetLogDirectory(), options.LogDirectory);
            Assert.False(options.LogToStdout, "Stdout should remain disabled without a plugin hook.");
            Assert.False(options.LogToStderr, "Stderr should remain disabled without a plugin hook.");
            Assert.Equal(SdkDefaultFileSizeLimitKilobytes, options.FileSizeLimitKilobytes);
            Assert.Equal(SdkDefaultMaxRetainedFiles, options.MaxRetainedFiles);
        }
    }

    [Theory]
    [InlineData("traces")]
    [InlineData("metrics")]
    [InlineData("logs")]
    public void WhenSdkSinksAreNone_Then_AgentProvidersDoNotCreateFallbackFiles(string signal)
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var scope = CreateScope(
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "none"),
            ("OTEL_DOTNET_AUTO_LOG_DIRECTORY", directory));
        var logger = OtelLogging.GetLogger();

        try
        {
            logger.Warning("before build", true);

            using var provider = BuildProvider(signal, null);
            OtelLogging.NotifySdkAvailable();
            OtelLogging.UseFallbackIfStillBuffering();

            logger.Error("after build", true);
            logger.Close();

            Assert.False(Directory.Exists(directory), "SDK sinks set to 'none' should not create a fallback log directory.");
        }
        finally
        {
            logger.Close();

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void WhenLaterBuilderConfigurationDisablesSinks_Then_NoFallbackFileIsCreated()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var scope = CreateScope(
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "stdout"),
            ("OTEL_DOTNET_AUTO_LOG_DIRECTORY", directory));
        var logger = OtelLogging.GetLogger();

        try
        {
            logger.Warning("before build", true);

            using var provider = Sdk.CreateTracerProviderBuilder()
                .ConfigureSelfDiagnostics(null)
                .ConfigureServices(services => services.Configure<SelfDiagnosticsOptions>(options => options.LogToStdout = false))
                .Build();
            OtelLogging.NotifySdkAvailable();
            OtelLogging.UseFallbackIfStillBuffering();

            logger.Error("after build", true);
            logger.Close();

            Assert.False(Directory.Exists(directory), "Disabling sinks in the builder should prevent fallback log directory creation.");
        }
        finally
        {
            logger.Close();

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Theory]
    [InlineData("traces", "info")]
    [InlineData("metrics", "info")]
    [InlineData("logs", "info")]
    [InlineData("traces", "none")]
    [InlineData("metrics", "none")]
    [InlineData("logs", "none")]
    public void WhenPluginLowersSdkLevel_Then_ManagedLoggerUsesTheConfiguredLevel(string signal, string initialLevel)
    {
        using var scope = CreateScope(
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "stdout"),
            ("OTEL_LOG_LEVEL", initialLevel));
        var logger = OtelLogging.GetLogger();

        Assert.False(logger.IsEnabled(AgentLogLevel.Debug), "Debug logging should be disabled before the plugin lowers the SDK level.");

        using var provider = BuildProvider(signal, CreatePluginManager<SelfDiagnosticsDebugPlugin>());
        OtelLogging.NotifySdkAvailable();

        Assert.True(logger.IsEnabled(AgentLogLevel.Debug), "The existing logger should use the plugin's Debug level.");
        Assert.True(OtelLogging.GetLogger("OpAmp").IsEnabled(AgentLogLevel.Debug), "A new OpAmp logger should use the plugin's Debug level.");
    }

#if NET
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhenILoggerProviderStarts_Then_ManagedLogsSwitchToSdkDiagnostics(bool useFallbackFirst)
    {
        using var scope = CreateScope(("OTEL_DOTNET_AUTO_LOGGER", "console"));
        var logger = Assert.IsType<SelfDiagnosticsLogger>(OtelLogging.GetLogger());

        if (useFallbackFirst)
        {
            OtelLogging.UseFallbackIfStillBuffering();
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ConfigureSelfDiagnostics(null).AddOpenTelemetry());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SelfDiagnosticsOptions>>().Value;

        Assert.True(options.LogToStdout, "The console logger setting should enable SDK stdout.");
        Assert.Equal(LogLevel.Information, options.MinimumLevel);

        var factory = provider.GetRequiredService<ILoggerFactory>();
        factory.CreateLogger("test");

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Sdk, logger.CurrentRoutingMode);
        Assert.True(logger.IsEnabled(AgentLogLevel.Information), "Provider startup should enable Information logging.");
        Assert.False(logger.IsEnabled(AgentLogLevel.Debug), "Provider startup should leave Debug logging disabled.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhenILoggerPluginEnablesDiagnostics_Then_ExistingAndLateManagedLoggersUseItsLevel(bool useFallbackFirst)
    {
        using var scope = CreateScope(("OTEL_LOG_LEVEL", "none"), ("OTEL_DOTNET_AUTO_LOGGER", "none"));
        var logger = OtelLogging.GetLogger();

        Assert.False(logger.IsEnabled(AgentLogLevel.Error), "Error logging should be disabled before the plugin enables diagnostics.");

        if (useFallbackFirst)
        {
            OtelLogging.UseFallbackIfStillBuffering();
        }

        SelfDiagnosticsOverridingPlugin.CallCount = 0;

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ConfigureSelfDiagnostics(CreatePluginManager<SelfDiagnosticsDebugPlugin>()).AddOpenTelemetry());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("test");

        Assert.Equal(1, SelfDiagnosticsOverridingPlugin.CallCount);
        Assert.True(logger.IsEnabled(AgentLogLevel.Debug), "The existing logger should use the plugin's Debug level.");
        Assert.True(OtelLogging.GetLogger("OpAmp").IsEnabled(AgentLogLevel.Debug), "A new OpAmp logger should use the plugin's Debug level.");
    }

    [Fact]
    public void WhenILoggerDiagnosticsAreDisabled_Then_NoManagedFallbackFileIsCreated()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var scope = CreateScope(
            ("OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS", "none"),
            ("OTEL_DOTNET_AUTO_LOG_DIRECTORY", directory));
        var logger = OtelLogging.GetLogger();

        try
        {
            logger.Warning("before build", true);

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.ConfigureSelfDiagnostics(null).AddOpenTelemetry());

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
            OtelLogging.UseFallbackIfStillBuffering();

            logger.Error("after build", true);
            logger.Close();

            Assert.False(Directory.Exists(directory), "Disabled ILogger diagnostics should not create a fallback log directory.");
        }
        finally
        {
            logger.Close();

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
#endif

    private static IDisposable BuildProvider(string signal, PluginManager? pluginManager)
    {
        if (signal == "traces")
        {
            return Sdk.CreateTracerProviderBuilder().ConfigureSelfDiagnostics(pluginManager).Build();
        }

        if (signal == "metrics")
        {
            return Sdk.CreateMeterProviderBuilder().ConfigureSelfDiagnostics(pluginManager).Build();
        }

        var factory = typeof(Sdk).GetMethod("CreateLoggerProviderBuilder", BindingFlags.Static | BindingFlags.NonPublic)!;
        var builder = (LoggerProviderBuilder)factory.Invoke(null, null)!;

        return builder.ConfigureSelfDiagnostics(pluginManager).Build();
    }

    private static PluginManager CreatePluginManager<TPlugin>()
        where TPlugin : IPlugin
    {
        var settings = new PluginsSettings();
        settings.Plugins.Add(typeof(TPlugin).AssemblyQualifiedName!);

        return new PluginManager(settings);
    }

    /// <summary>
    /// Clears every variable the configurator reads, then applies the given ones, so a test is
    /// independent of the machine it runs on.
    /// </summary>
    private static Util.EnvironmentScope CreateScope(params (string Name, string? Value)[] variables)
    {
        var values = AllVariables.ToDictionary(name => name, _ => (string?)null);

        foreach (var (name, value) in variables)
        {
            values[name] = value;
        }

        var scope = new Util.EnvironmentScope(values);
        OtelLogging.Reset();

        return scope;
    }

#pragma warning disable CA1034 // Nested types should not be visible. Public so that PluginManager can instantiate them by assembly-qualified name.
#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin purposes.
    public class SelfDiagnosticsOverridingPlugin : IPlugin, IConfigureSelfDiagnosticsOptions
    {
        public static int CallCount { get; set; }

        protected virtual LogLevel MinimumLevel => LogLevel.Error;

        public void Initializing()
        {
        }

        public void Initialized()
        {
        }

        public void ConfigureSelfDiagnosticsOptions(SelfDiagnosticsOptions options)
        {
#if NET
            ArgumentNullException.ThrowIfNull(options);
#else
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
#endif

            CallCount++;
            options.MinimumLevel = MinimumLevel;
            options.LogToStderr = true;
        }
    }

    public class SelfDiagnosticsDebugPlugin : SelfDiagnosticsOverridingPlugin
    {
        protected override LogLevel MinimumLevel => LogLevel.Debug;
    }

    public class SelfDiagnosticsThrowingPlugin : IPlugin, IConfigureSelfDiagnosticsOptions
    {
        public void Initializing()
        {
        }

        public void Initialized()
        {
        }

        public void ConfigureSelfDiagnosticsOptions(SelfDiagnosticsOptions options)
        {
            throw new InvalidOperationException("Plugin failure for the test.");
        }
    }
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin purposes.
#pragma warning restore CA1034 // Nested types should not be visible.
}
