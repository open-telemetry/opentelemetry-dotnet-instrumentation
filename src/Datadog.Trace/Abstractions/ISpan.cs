using System;

namespace Datadog.Trace.Abstractions
{
    internal interface ISpan
    {
        string ResourceName { get; set; }

        string Type { get; set; }

        /// <summary>
        /// Gets or sets the span's execution status
        /// </summary>
        SpanStatus Status { get; set; }

        ISpan SetTag(string key, string value);

        string GetTag(string key);

        void SetException(Exception exception);
    }
}
