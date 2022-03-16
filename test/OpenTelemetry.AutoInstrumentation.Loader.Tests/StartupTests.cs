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
