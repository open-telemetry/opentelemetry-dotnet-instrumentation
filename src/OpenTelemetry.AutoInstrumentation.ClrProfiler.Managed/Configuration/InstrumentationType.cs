namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Enum representing all instrumentations.
    /// </summary>
    public enum InstrumentationType
    {
        /// <summary>
        /// HttpClient instrumentation.
        /// </summary>
        HttpClient,

        /// <summary>
        /// ASP.NET instrumentation.
        /// </summary>
        AspNet,

        /// <summary>
        /// SqlClient instrumentation.
        /// </summary>
        SqlClient
    }
}
