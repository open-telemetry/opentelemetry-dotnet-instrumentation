using System;
using System.IO;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Loader;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Loader.Tests
{
    public class StartupTests
    {
        [Fact]
        public void Ctor_LoadsManagedAssembly()
        {
            var director = Directory.GetCurrentDirectory();
            Environment.SetEnvironmentVariable("OTEL_DOTNET_TRACER_HOME", @$"{director}\Profiler");

            var exception = Record.Exception(() => Startup.ManagedProfilerDirectory);
            // That means the assembly was loaded successfully and Initialize method was called.
            Assert.Null(exception);
        }
    }
}
