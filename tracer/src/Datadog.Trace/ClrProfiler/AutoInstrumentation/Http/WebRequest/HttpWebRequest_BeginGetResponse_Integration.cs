using System;
using System.ComponentModel;
using System.Net;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.WebRequest
{
    /// <summary>
    /// CallTarget integration for HttpWebRequest.BeginGetResponse
    /// We only instrument .NET Framework - .NET Core uses an HttpClient
    /// internally, which is already instrumented
    /// </summary>
    [InstrumentMethod(
        AssemblyName = WebRequestCommon.NetFrameworkAssembly,
        TypeName = WebRequestCommon.HttpWebRequestTypeName,
        MethodName = MethodName,
        ReturnTypeName = ClrNames.IAsyncResult,
        ParameterTypeNames = new[] { ClrNames.AsyncCallback, ClrNames.Object },
        MinimumVersion = WebRequestCommon.Major4,
        MaximumVersion = WebRequestCommon.Major4,
        IntegrationName = WebRequestCommon.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HttpWebRequest_BeginGetResponse_Integration
    {
        private const string MethodName = "BeginGetResponse";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="callback">The AsyncCallback delegate</param>
        /// <param name="state">An object containing state information for this asynchronous request</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, AsyncCallback callback, object state)
        {
            if (instance is HttpWebRequest request && WebRequestCommon.IsTracingEnabled(request))
            {
                var tracer = Tracer.Instance;

                // We may have already set headers
                var propagator = tracer.Propagator;
                var requestContext = propagator.Extract(request.Headers, (headers, key) => headers.GetValues(key));
                if (requestContext is null || requestContext.TraceId == TraceId.Zero)
                {
                    var spanContext = ScopeFactory.CreateHttpSpanContext(tracer, WebRequestCommon.IntegrationId);

                    if (spanContext != null)
                    {
                        // Add distributed tracing headers to the HTTP request.
                        // We don't want to set an active scope now, because it's possible that EndGetResponse will never be called.
                        // Instead, we generate a spancontext and inject it in the headers. EndGetResponse will fetch them and create an active scope with the right id.
                        propagator.Inject(spanContext, request.Headers, (headers, key, value) => headers.Set(key, value));
                    }
                }
            }

            return CallTargetState.GetDefault();
        }
    }
}
