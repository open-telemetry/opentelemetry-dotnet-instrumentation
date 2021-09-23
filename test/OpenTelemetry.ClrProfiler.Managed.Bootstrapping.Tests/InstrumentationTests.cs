using System;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Bootstrapping.Tests
{
    public class InstrumentationTests
    {
        private readonly ActivitySource _otelActivitySource = new("OpenTelemetry.ClrProfiler.*");
        private readonly ActivitySource _customActivitySource = new("Custom");

        public InstrumentationTests()
        {
            // Exporter should always be set to a valid value
            Environment.SetEnvironmentVariable("OTEL_EXPORTER", "zipkin");
        }

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
