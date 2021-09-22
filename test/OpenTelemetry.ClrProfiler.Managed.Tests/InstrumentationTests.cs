using System;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Tests
{
    public class InstrumentationTests : BaseInstrumentationTests
    {
        [Fact]
        public void Initialize_WithDisabledFlag_DoesNotCreateTracerProvider()
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_LOAD_AT_STARTUP", "false");

            Instrumentation.Initialize();
            var otelActivity = OtelActivitySource.StartActivity("OtelActivity");
            var customActivity = CustomActivitySource.StartActivity("CustomActivity");

            Assert.Null(otelActivity);
            Assert.Null(customActivity);
        }

        [Fact]
        public void Initialize_WithDefaultFlag_CreatesTracerProvider()
        {
            var exception = Record.Exception(Instrumentation.Initialize);

            Instrumentation.Initialize();
            var otelActivity = OtelActivitySource.StartActivity("OtelActivity");
            var customActivity = CustomActivitySource.StartActivity("CustomActivity");

            Assert.NotNull(otelActivity);
            Assert.Null(customActivity);
        }

        [Fact]
        public void Initialize_WithEnabledFlag_CreatesTracerProvider()
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_LOAD_AT_STARTUP", "true");

            Instrumentation.Initialize();
            var otelActivity = OtelActivitySource.StartActivity("OtelActivity");
            var customActivity = CustomActivitySource.StartActivity("CustomActivity");

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
            var otelActivity = OtelActivitySource.StartActivity("OtelActivity");
            var customActivity = CustomActivitySource.StartActivity("CustomActivity");

            Assert.Null(otelActivity);
            Assert.NotNull(customActivity);

            tracer.Dispose();
        }
    }
}
