namespace OpenTelemetry.AutoInstrumentation.Logging
{
    /// <summary>
    /// Specifies the meaning and relative importance of a log event.
    /// </summary>
    internal enum LogLevel
    {
        /// <summary>
        /// Functionality is unavailable, invariants are broken
        /// or data is lost.
        /// </summary>
        Error,

        /// <summary>
        /// Service is degraded or endangered.
        /// </summary>
        Warning,

        /// <summary>
        /// The lifeblood of operational intelligence - things
        /// happen.
        /// </summary>
        Information,

        /// <summary>
        /// Internal system events that aren't necessarily
        /// observable from the outside.
        /// </summary>
        Debug,
    }
}
