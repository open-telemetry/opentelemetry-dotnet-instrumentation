using System;
using System.Net;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Propagation;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.WebRequest
{
    internal class WebRequestCommon
    {
        internal const string NetFrameworkAssembly = "System";
        internal const string NetCoreAssembly = "System.Net.Requests";

        internal const string HttpWebRequestTypeName = "System.Net.HttpWebRequest";
        internal const string WebRequestTypeName = "System.Net.WebRequest";
        internal const string WebResponseTypeName = "System.Net.WebResponse";
        internal const string WebResponseTask = "System.Threading.Tasks.Task`1<" + WebResponseTypeName + ">";

        internal const string Major2 = "2";
        internal const string Major4 = "4";
        internal const string Major5 = "5";

        internal const string IntegrationName = nameof(IntegrationIds.WebRequest);
        internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState GetResponse_OnMethodBegin<TTarget>(TTarget instance)
        {
            if (instance is HttpWebRequest request && IsTracingEnabled(request))
            {
                Tracer tracer = Tracer.Instance;

                // Check if any headers were injected by a previous call to GetRequestStream
                var spanContext = tracer.Propagator.Extract(request.Headers.Wrap());

                Scope scope = null;

                try
                {
                    scope = ScopeFactory.CreateOutboundHttpScope(tracer, request.Method, request.RequestUri, IntegrationId, out var tags, spanContext?.SpanId);

                    if (scope != null)
                    {
                        // add distributed tracing headers to the HTTP request
                        tracer.Propagator.Inject(scope.Span.Context, request.Headers.Wrap());

                        return new CallTargetState(scope);
                    }
                }
                catch
                {
                    scope?.Dispose();
                    throw;
                }
            }

            return CallTargetState.GetDefault();
        }

        internal static bool IsTracingEnabled(System.Net.WebRequest request)
        {
            // check if tracing is disabled for this request via http header
            string value = request.Headers[CommonHttpHeaderNames.TracingEnabled];
            return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }
    }
}
