using System;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Bootstrapping.Tests
{
    /// <summary>
    /// When you add a new tests or change the name of one of the existing ones please remember to reflect the changes
    /// in the build project, by updating the list in RunBootstrappingTests method of Build.Steps.cs.
    /// </summary>
    public class InstrumentationTests
    {
        private readonly ActivitySource _otelActivitySource = new("OpenTelemetry.ClrProfiler.*");
        private readonly ActivitySource _customActivitySource = new("Custom");

        [Fact]
        public void Initialize_WithDisabledFlag_DoesNotCreateTracerProvider()
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_LOAD_AT_STARTUP", "false");

            Instrumentation.Initialize();
            var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
            var customActivity = _customActivitySource.StartActivity("CustomActivity");

            Assert.Null(otelActivity);
            Assert.Null(customActivity);
        }

        [Fact]
        public void Initialize_WithDefaultFlag_CreatesTracerProvider()
        {
            Instrumentation.Initialize();

            Instrumentation.Initialize();
            var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
            var customActivity = _customActivitySource.StartActivity("CustomActivity");

            Assert.NotNull(otelActivity);
            Assert.Null(customActivity);
        }

        [Fact]
        public void Initialize_WithEnabledFlag_CreatesTracerProvider()
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_LOAD_AT_STARTUP", "true");

            Instrumentation.Initialize();
            var otelActivity = _otelActivitySource.StartActivity("OtelActivity");
            var customActivity = _customActivitySource.StartActivity("CustomActivity");

            Assert.NotNull(otelActivity);
            Assert.Null(customActivity);
        }

        [Fact]
        public void Initialize_WithPreviouslyCreatedTracerProvider_WorksCorrectly()
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_LOAD_AT_STARTUP", "false");
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
    }
}
