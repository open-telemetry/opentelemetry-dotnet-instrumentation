// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.Transmission;
using OtlpCollector = OpenTelemetry.Proto.Collector.Profiles.V1Experimental;
using OtlpResource = OpenTelemetry.Proto.Resource.V1;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

/// <summary>
/// Exporter consuming Tuple&lt;byte[], int, string&gt; and exporting the data using
/// the OpenTelemetry protocol (OTLP).
/// </summary>
internal sealed class OtlpProfilerExporter : BaseExporter<Tuple<byte[], int, string>>
{
    private readonly OtlpExporterTransmissionHandler<OtlpCollector.ExportProfilesServiceRequest> transmissionHandler;
    private readonly OtlpProfilerTransformer otlpProfilerTransformer;

    private OtlpResource.Resource? processResource;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpProfilerExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    /// <param name="otlpProfilerTransformer"><see cref="OtlpProfilerTransformer"/>.</param>
    public OtlpProfilerExporter(OtlpExporterOptions options, OtlpProfilerTransformer otlpProfilerTransformer)
        : this(options, otlpProfilerTransformer, sdkLimitOptions: new(), experimentalOptions: new(), transmissionHandler: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpProfilerExporter"/> class.
    /// </summary>
    /// <param name="exporterOptions"><see cref="OtlpExporterOptions"/>.</param>
    /// <param name="otlpProfilerTransformer"><see cref="OtlpProfilerTransformer"/>.</param>
    /// <param name="sdkLimitOptions"><see cref="SdkLimitOptions"/>.</param>
    /// <param name="experimentalOptions"><see cref="ExperimentalOptions"/>.</param>
    /// <param name="transmissionHandler"><see cref="OtlpExporterTransmissionHandler{T}"/>.</param>
    internal OtlpProfilerExporter(
        OtlpExporterOptions exporterOptions,
        OtlpProfilerTransformer otlpProfilerTransformer,
        SdkLimitOptions sdkLimitOptions,
        ExperimentalOptions experimentalOptions,
        OtlpExporterTransmissionHandler<OtlpCollector.ExportProfilesServiceRequest>? transmissionHandler = null)
    {
        Debug.Assert(exporterOptions != null, "exporterOptions was null");
        Debug.Assert(otlpProfilerTransformer != null, "otlpProfilerTransformer was null");
        Debug.Assert(sdkLimitOptions != null, "sdkLimitOptions was null");
        Debug.Assert(experimentalOptions != null, "experimentalOptions was null");

        this.transmissionHandler = transmissionHandler ?? exporterOptions!.GetProfilesExportTransmissionHandler(experimentalOptions!);

        this.otlpProfilerTransformer = otlpProfilerTransformer!;
    }

    internal OtlpResource.Resource ProcessResource
        => this.processResource ??= this.ParentProvider.GetResource().ToOtlpResource();

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Tuple<byte[], int, string>> profilerBatch)
    {
        // Prevents the exporter's gRPC and HTTP operations from being instrumented.
        using var scope = SuppressInstrumentationScope.Begin();

        try
        {
            OtlpCollector.ExportProfilesServiceRequest? request = this.otlpProfilerTransformer.BuildExportRequest(this.ProcessResource, profilerBatch);

            if (request == null)
            {
                return ExportResult.Success;
            }

            if (!this.transmissionHandler.TrySubmitRequest(request))
            {
                return ExportResult.Failure;
            }
        }
        catch (Exception ex)
        {
            OpenTelemetryProtocolExporterEventSource.Log.ExportMethodException(ex);
            return ExportResult.Failure;
        }

        return ExportResult.Success;
    }

    /// <inheritdoc />
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        return this.transmissionHandler?.Shutdown(timeoutMilliseconds) ?? true;
    }
}
#endif
