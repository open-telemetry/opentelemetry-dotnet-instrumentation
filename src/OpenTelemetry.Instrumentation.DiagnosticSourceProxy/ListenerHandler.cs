using System.Collections.Generic;
using System.Diagnostics;

namespace OpenTelemetry.Instrumentation.DiagnosticSourceProxy
{
    internal class ListenerHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListenerHandler"/> class.
        /// </summary>
        /// <param name="sourceName">The name of the <see cref="ListenerHandler"/>.</param>
        public ListenerHandler(string sourceName)
        {
            SourceName = sourceName;
        }

        /// <summary>
        /// Gets the name of the <see cref="ListenerHandler"/>.
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ListenerHandler"/> supports NULL <see cref="Activity"/>.
        /// </summary>
        public virtual bool SupportsNullActivity { get; }

        /// <summary>
        /// Method called for an event with the suffix 'Start'.
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> to be started.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnStartActivity(Activity activity, KeyValuePair<string, object> payload)
        {
        }

        /// <summary>
        /// Method called for an event with the suffix 'Stop'.
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> to be stopped.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnStopActivity(Activity activity, KeyValuePair<string, object> payload)
        {
        }

        /// <summary>
        /// Method called for an event with the suffix 'Exception'.
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/>.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnException(Activity activity, KeyValuePair<string, object> payload)
        {
        }

        /// <summary>
        /// Method called for an event which does not have 'Start', 'Stop' or 'Exception' as suffix.
        /// </summary>
        /// <param name="name">Custom name.</param>
        /// <param name="activity">The <see cref="Activity"/> to be processed.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnCustom(string name, Activity activity, KeyValuePair<string, object> payload)
        {
        }
    }
}
