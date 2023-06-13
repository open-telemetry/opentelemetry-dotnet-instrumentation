// <copyright file="LoaderTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using Xunit;
using Xunit.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.Loader.Tests;

public class LoaderTests
{
    private ITestOutputHelper _testOutput;

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
        var srcDir = Path.Combine(profilerDirectory, "net6.0");
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
            .FirstOrDefault(n => n != null && n.StartsWith("OpenTelemetry.AutoInstrumentation,"));

        Assert.NotNull(openTelemetryAutoInstrumentationAssembly);
    }
}
