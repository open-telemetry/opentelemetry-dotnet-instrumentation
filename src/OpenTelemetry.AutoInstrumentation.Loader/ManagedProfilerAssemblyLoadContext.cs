#if NETCOREAPP
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.ClrProfiler.Managed.Loader
{
    internal class ManagedProfilerAssemblyLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
#endif
