using System;
using System.Diagnostics;

namespace OpenTelemetry.ClrProfiler.Managed.Tests
{
    public class BaseInstrumentationTests : IDisposable
    {
        protected BaseInstrumentationTests()
        {
            // Exporter should always be set to a valid value
            Environment.SetEnvironmentVariable("OTEL_EXPORTER", "zipkin");
            OtelActivitySource = new ActivitySource("OpenTelemetry.ClrProfiler.*");
            CustomActivitySource = new ActivitySource("Custom");
        }

        protected ActivitySource OtelActivitySource { get; }

        protected ActivitySource CustomActivitySource { get; }

        public void Dispose()
        {
            Instrumentation.DisposeTracerProvider();
        }
    }
}
