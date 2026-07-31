// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;

namespace IntegrationTests.Helpers;

/// <summary>
/// Forces the Google.Protobuf descriptor graph to be initialized once, single-threaded,
/// before any mock collector starts serving requests.
/// <para>
/// Without this, a collector's HTTP server thread parsing an incoming OTLP message can run
/// concurrently with a test thread formatting a protobuf message (for example
/// <c>AnyValue.ToString()</c> via <see cref="JsonFormatter"/> when building an expectation
/// description). Both paths trigger Google.Protobuf type initializers (notably
/// <c>FeatureSetDescriptor..cctor</c> and the generated <c>*Reflection..cctor</c> types), and
/// concurrent type initialization can deadlock the whole test host until the hang-dump timer
/// aborts the run. Running the initialization once up front avoids that race.
/// </para>
/// </summary>
internal static class ProtobufDescriptorWarmup
{
    static ProtobufDescriptorWarmup()
    {
        // Touch the descriptors for the OTLP message types the collectors parse.
        _ = ExportTraceServiceRequest.Descriptor;
        _ = ExportLogsServiceRequest.Descriptor;
        _ = ExportMetricsServiceRequest.Descriptor;
        _ = ExportProfilesServiceRequest.Descriptor;
        _ = AnyValue.Descriptor;

        // Exercise the JsonFormatter path (the observed deadlock site) once, single-threaded.
        _ = JsonFormatter.Default.Format(new AnyValue { StringValue = "warmup" });
    }

    /// <summary>
    /// Ensures the protobuf descriptor graph has been initialized. Referencing this member
    /// runs the static constructor above exactly once (the CLR guarantees it is thread-safe
    /// and runs a single time), so callers can invoke it from every collector constructor.
    /// </summary>
    public static void Ensure()
    {
    }
}
