using System;
using System.IO;
using System.Linq;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Loader.Tests;

public class StartupTests
{
    [Fact]
    public void Ctor_LoadsManagedAssembly()
    {
        var directory = Directory.GetCurrentDirectory();
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_HOME", Path.Combine(directory, "..", "Profiler"));

        var exception = Record.Exception(() => AutoInstrumentation.Loader.Startup.ManagedProfilerDirectory);

        // That means the assembly was loaded successfully and Initialize method was called.
        Assert.Null(exception);

        var openTelemetryAutoInstrumentationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .FirstOrDefault(n => n.StartsWith("OpenTelemetry.AutoInstrumentation,"));

        Assert.NotNull(openTelemetryAutoInstrumentationAssembly);
    }
}
