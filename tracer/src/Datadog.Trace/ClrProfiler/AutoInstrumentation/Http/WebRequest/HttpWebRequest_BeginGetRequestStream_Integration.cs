using System;
using System.ComponentModel;
using System.Net;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.WebRequest
{
    /// <summary>
    /// CallTarget integration for HttpWebRequest.BeginGetRequestStream
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
    [InstrumentMethod(
        AssemblyName = WebRequestCommon.NetCoreAssembly,
        TypeName = WebRequestCommon.HttpWebRequestTypeName,
        MethodName = MethodName,
        ReturnTypeName = ClrNames.IAsyncResult,
        ParameterTypeNames = new[] { ClrNames.AsyncCallback, ClrNames.Object },
        MinimumVersion = WebRequestCommon.Major4,
        MaximumVersion = WebRequestCommon.Major5,
        IntegrationName = WebRequestCommon.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HttpWebRequest_BeginGetRequestStream_Integration
    {
        private const string MethodName = "BeginGetRequestStream";

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

                if (tracer.Settings.IsIntegrationEnabled(WebRequestCommon.IntegrationId))
                {
                    var spanContext = ScopeFactory.CreateHttpSpanContext(tracer, WebRequestCommon.IntegrationId);

                    if (spanContext != null)
                    {
                        // Add distributed tracing headers to the HTTP request.
                        // The expected sequence of calls is GetRequestStream -> GetResponse. Headers can't be modified after calling GetRequestStream.
                        // At the same time, we don't want to set an active scope now, because it's possible that GetResponse will never be called.
                        // Instead, we generate a spancontext and inject it in the headers. GetResponse will fetch them and create an active scope with the right id.
                        tracer.Propagator.Inject(spanContext, request.Headers, (headers, key, value) => headers.Set(key, value));
                    }
                }
            }

            return CallTargetState.GetDefault();
        }
    }
}
