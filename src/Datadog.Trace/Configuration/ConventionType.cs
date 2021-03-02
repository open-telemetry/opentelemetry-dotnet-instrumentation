namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Semantic convention used to when defining operation names, span tags, statuses etc.
    /// </summary>
    public enum ConventionType
    {
        /// <summary>
        /// The default OpenTelemtry convention.
        /// </summary>
        Default,

        /// <summary>
        /// The OpenTelemetry convention.
        /// </summary>
        OpenTelemetry = Default,

        /// <summary>
        /// The Datadog convention.
        /// </summary>
        Datadog,
    }
}