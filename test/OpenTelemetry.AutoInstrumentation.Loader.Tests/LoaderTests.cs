// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using Xunit.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.Loader.Tests;

public class LoaderTests(ITestOutputHelper testOutput)
{
    [Fact]
    public void Ctor_LoadsManagedAssembly()
    {
        var directory = Directory.GetCurrentDirectory();
        var profilerDirectory = Path.Combine(directory, "..", "Profiler");
        testOutput.WriteLine($"profilerDirectory={profilerDirectory}");

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

        var exception = Record.Exception(() => new Loader());

        // That means the assembly was loaded successfully and Initialize method was called.
        Assert.Null(exception);

        Assert.Contains(
            AppDomain.CurrentDomain.GetAssemblies(),
            it => it.FullName?.StartsWith("OpenTelemetry.AutoInstrumentation,", StringComparison.Ordinal) == true);
    }
}
