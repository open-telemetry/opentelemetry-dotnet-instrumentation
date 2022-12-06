// <copyright file="StartupTests.cs" company="OpenTelemetry Authors">
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
        var profilerDirectory = Path.Combine(directory, "..", "Profiler");

#if NETFRAMEWORK
        if (Directory.Exists(Path.Combine(profilerDirectory, "net462")))
        {
            Directory.Move(Path.Combine(profilerDirectory, "net462"), Path.Combine(profilerDirectory, "netfx"));
        }
#else
        if (Directory.Exists(Path.Combine(profilerDirectory, "net6.0")))
        {
            Directory.Move(Path.Combine(profilerDirectory, "net6.0"), Path.Combine(profilerDirectory, "net"));
        }
#endif

        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_HOME", profilerDirectory);

        var exception = Record.Exception(() => new AutoInstrumentation.Loader.Startup());

        // That means the assembly was loaded successfully and Initialize method was called.
        Assert.Null(exception);

        var openTelemetryAutoInstrumentationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .FirstOrDefault(n => n != null && n.StartsWith("OpenTelemetry.AutoInstrumentation,"));

        Assert.NotNull(openTelemetryAutoInstrumentationAssembly);
    }
}
