namespace OpenTelemetry.DynamicActivityBinding
{
    /// <summary>
    /// The <see cref="DynamicLoader"/> is used to load the <c>System.Diagnostics.DiagnosticSource</c> assembly
    /// while handling the many possible scenarios, eg.: already loaded by the process, available on default
    /// load context, etc.
    /// </summary>
    internal static class DynamicLoader
    {
        // Assembly System.Diagnostics.DiagnosticSource version 4.0.2.0 is the first official version of that assembly
        // that contains Activity previous versions contained DiagnosticSource only.
        // That version was shipped in the System.Diagnostics.DiagnosticSource NuGet version 4.4.0 on 2017-06-28.
        // See https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/4.4.0
        // It is highly unlikey that an application references an older version of DiagnosticSource.
        // However, if it does, we will not instrument it.

        /// <summary>
        /// Returns the result of the attempt to load the <c>System.Diagnostics.DiagnosticSource</c> assembly.
        /// </summary>
        /// <remarks>
        /// It can be called concurrently the implementation will ensure that only a single attempt is performed
        /// and all concurrent and subsequent calls return the result of that attempt.
        /// </remarks>
        /// <returns>
        /// True if the <c>System.Diagnostics.DiagnosticSource</c> assembly was successfully
        /// loaded or false otherwise.
        /// </returns>
        public static bool EnsureInitialized()
        {
            // TODO: Always false until actual assembly load is in implemented.
            return false;
        }
    }
}
