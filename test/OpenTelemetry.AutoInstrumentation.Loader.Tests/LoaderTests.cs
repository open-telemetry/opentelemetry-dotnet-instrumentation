// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using Xunit.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.Loader.Tests;

public class LoaderTests
{
    private readonly ITestOutputHelper _testOutput;

    public LoaderTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public void Ctor_LoadsManagedAssembly()
    {
        var directory = Directory.GetCurrentDirectory();
        var profilerDirectory = Path.Combine(directory, "..", "Profiler");
        _testOutput.WriteLine($"profilerDirectory={profilerDirectory}");

#if NETFRAMEWORK
        var srcDir = Path.Combine(profilerDirectory, "net462");
        var dstDir = Path.Combine(profilerDirectory, "netfx");
#else
        var srcDir = Path.Combine(profilerDirectory, "net8.0");
        var dstDir = Path.Combine(profilerDirectory, "net");
#endif

        if (Directory.Exists(srcDir) && !Directory.Exists(dstDir))
        {
            Directory.Move(srcDir, dstDir);
        }

        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_HOME", profilerDirectory);

        var exception = Record.Exception(() => new AutoInstrumentation.Loader.Loader());

        // That means the assembly was loaded successfully and Initialize method was called.
        Assert.Null(exception);

        var openTelemetryAutoInstrumentationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .FirstOrDefault(n => n != null && n.StartsWith("OpenTelemetry.AutoInstrumentation,", StringComparison.Ordinal));

        Assert.NotNull(openTelemetryAutoInstrumentationAssembly);
    }
}
