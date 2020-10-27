using System;
using System.Collections.Generic;
using System.Net;

using Datadog.Trace;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.Vendors.MessagePack;

using OpenTelemetry.AutoInstrumentation.ActivityCollector;
using OpenTelemetry.AutoInstrumentation.ActivityExporter;
using OpenTelemetry.DynamicActivityBinding;

namespace Datadog.AutoInstrumentation.ActivityExporter
{
#pragma warning disable SA1118 // Parameter must not span multiple lines

    /// <summary>
    /// Currently not actually used in prod. Contains APIs for an Actvity Export POC prototype.
    /// </summary>
    internal class DatadogAgentMessagePackActivityExporter : ActivityExporterBase
    {
        private readonly FormatterResolverWrapper _formatterResolver = new FormatterResolverWrapper(SpanFormatterResolver.Instance);

        public DatadogAgentMessagePackActivityExporter()
            : base(isExportTracesSupported: true, isExportActivitiesSupported: false)
        {
        }

        protected override ExportResult ExportActivitiesImpl(IReadOnlyCollection<ActivityStub> spans)
        {
            throw new NotSupportedException($"{nameof(DatadogAgentMessagePackActivityExporter)} only supports exporting Traces, not stand-alone spans.");
        }

        protected override ExportResult ExportTracesImpl(IReadOnlyCollection<LocalTrace> traces)
        {
            if (traces == null || traces.Count == 0)
            {
                return ExportResult.CreateSuccess(isTraceExport: true, requestedTraceCount: 0, requestedActivityCount: 0);
            }

            // @ToDo!
            // Hacky, just trying to get something up and running
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(new Uri("http://localhost:8126"), "/v0.4/traces"));

            // Default headers
            request.Headers.Add(AgentHttpHeaderNames.Language, ".NET");
            request.Headers.Add(AgentHttpHeaderNames.TracerVersion, TracerConstants.AssemblyVersion);

            // don't add automatic instrumentation to requests from this HttpClient
            request.Headers.Add(HttpHeaderNames.TracingEnabled, "false");

            request.Headers.Add(AgentHttpHeaderNames.TraceCount, traces.Count.ToString());
            request.Method = "POST";

            request.ContentType = "application/msgpack";
            using (var requestStream = request.GetRequestStream())
            {
                MessagePackSerializer.Serialize(requestStream, traces, _formatterResolver);
            }

            int requestedActivityCount = -1;  // We should properly calculate it at serialization time.

            try
            {
                var httpWebResponse = (HttpWebResponse)request.GetResponse();

                if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                {
                    return ExportResult.CreateFailure(
                                            isTraceExport: true,
                                            requestedTraceCount: traces.Count,
                                            requestedActivityCount: requestedActivityCount,
                                            ExportResult.Status.Failure_Unspecified,
                                            $"Trace ingestion endpoint responded with a failure status code"
                                          + $" \"{httpWebResponse.StatusCode.ToString()}\" ({(int)httpWebResponse.StatusCode}).");
                }
                else
                {
                    return ExportResult.CreateSuccess(
                                            isTraceExport: true,
                                            requestedTraceCount: traces.Count,
                                            requestedActivityCount: requestedActivityCount);
                }
            }
            catch (Exception ex)
            {
                return ExportResult.CreateFailure(
                                            isTraceExport: true,
                                            requestedTraceCount: traces.Count,
                                            requestedActivityCount: requestedActivityCount,
                                            ExportResult.Status.Failure_Unspecified,
                                            ex);
            }
        }

        protected override void ShutdownImpl()
        {
        }
    }
#pragma warning restore SA1118 // Parameter must not span multiple lines
}
