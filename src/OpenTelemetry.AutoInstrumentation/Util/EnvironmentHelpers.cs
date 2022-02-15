using System;
using OpenTelemetry.ClrProfiler.Managed.Logging;

namespace OpenTelemetry.ClrProfiler.Managed.Util
{
    /// <summary>
    /// Helpers to access environment variables
    /// </summary>
    internal static class EnvironmentHelpers
    {
        private static readonly ILogger Logger = ConsoleLogger.Create(typeof(EnvironmentHelpers));

        /// <summary>
        /// Safe wrapper around Environment.GetEnvironmentVariable
        /// </summary>
        /// <param name="key">Name of the environment variable to fetch</param>
        /// <param name="defaultValue">Value to return in case of error</param>
        /// <returns>The value of the environment variable, or the default value if an error occured</returns>
        public static string GetEnvironmentVariable(string key, string defaultValue = null)
        {
            try
            {
                return Environment.GetEnvironmentVariable(key);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error while reading environment variable {EnvironmentVariable}", key);
            }

            return defaultValue;
        }
    }
}
