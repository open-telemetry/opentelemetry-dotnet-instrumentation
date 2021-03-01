namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Convention used to when defining operation names, span tags, statuses etc.
    /// </summary>
    public enum ConventionType
    {
        /// <summary>
        /// The default OpenTelemtry exporter.
        /// </summary>
        Default,

        /// <summary>
        /// The Datadog exporter.
        /// </summary>
        Datadog,
    }
}