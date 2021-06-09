// ReSharper disable InconsistentNaming - Name is used for integration names
namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Enum representing supported integrations.
    /// </summary>
    public enum IntegrationIds
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
        SqlClient,

        /// <summary>
        /// MongoDb instrumentation.
        /// </summary>
        MongoDb
    }
}
