using System;
using System.IO;
using System.Linq;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Loader.Tests
{
    public class StartupTests
    {
        [Fact]
        public void Ctor_LoadsManagedAssembly()
        {
            var directory = Directory.GetCurrentDirectory();
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_HOME", Path.Combine(directory, "..", "Profiler"));

            var exception = Record.Exception(() => Startup.ManagedProfilerDirectory);

            // That means the assembly was loaded successfully and Initialize method was called.
            Assert.Null(exception);

            var clrProfilerManagedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.FullName)
                .FirstOrDefault(n => n.StartsWith("OpenTelemetry.ClrProfiler.Managed,"));

            Assert.NotNull(clrProfilerManagedAssembly);
        }
    }
}
