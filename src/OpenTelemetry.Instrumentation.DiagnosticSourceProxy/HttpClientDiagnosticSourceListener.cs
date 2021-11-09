using System.Collections.Generic;
using System.Diagnostics;

namespace OpenTelemetry.Instrumentation.DiagnosticSourceProxy
{
    internal class HttpClientDiagnosticSourceListener : ListenerHandler
    {
        private const string DiagnosticSourceName = "HttpHandlerDiagnosticListener";

        public HttpClientDiagnosticSourceListener()
            : base(DiagnosticSourceName)
        {
        }

        public override void OnCustom(string name, Activity activity, KeyValuePair<string, object> value)
        {
        }

        public override void OnException(Activity activity, KeyValuePair<string, object> value)
        {
        }

        public override void OnStartActivity(Activity activity, KeyValuePair<string, object> value)
        {
            // TODO: Clone the Activity from older version of DiagnosticSource to
            // OpenTelemetry referenced DiagnosticSource using CreateActivity or StartActivity.
        }

        public override void OnStopActivity(Activity activity, KeyValuePair<string, object> value)
        {
            // TODO: Stop the cloned Activity.
        }
    }
}
