using System;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed
{
    /// <summary>
    /// A class that attempts to load the OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed .NET assembly.
    /// </summary>
    public partial class Startup
    {
        internal static string[] ManagedProfilerDirectories { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="Startup"/> class.
        /// This method also attempts to load the OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed .NET assembly.
        /// </summary>
        public static void SetupResolver()
        {
            ManagedProfilerDirectories = ResolveManagedProfilerDirectory().ToArray();
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
        }
    }
}
