// <copyright file="NugetTestHelper.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace NuGetPackagesTests;

public abstract class NugetTestHelper : TestHelper
{
    protected NugetTestHelper(string testApplicationName, ITestOutputHelper output, string testApplicationType = "nuget-packages")
        : base(testApplicationName, output, testApplicationType)
    {
    }

    public override Process? StartTestApplication(TestSettings? testSettings = null)
    {
        testSettings ??= new();

        var instrumentationScriptExtension = EnvironmentTools.IsWindows() ? ".cmd" : ".sh";
        var instrumentationScriptPath = Path.Combine(EnvironmentHelper.GetTestApplicationApplicationOutputDirectory(testSettings.PackageVersion, testSettings.Framework), "instrument") + instrumentationScriptExtension;

        if (!File.Exists(instrumentationScriptPath))
        {
            throw new Exception($"instrumentation script not found: {instrumentationScriptPath}");
        }

        // get path to test application that the profiler will attach to
        var testApplicationPath = EnvironmentHelper.GetTestApplicationPath(testSettings.PackageVersion, testSettings.Framework);
        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"application not found: {testApplicationPath}");
        }

        Output.WriteLine($"Starting Application: {instrumentationScriptPath} {testApplicationPath}");

        var args = $"{(EnvironmentHelper.IsCoreClr() ? EnvironmentHelper.GetTestApplicationExecutionSource() : string.Empty)} {testApplicationPath} {testSettings.Arguments ?? string.Empty}";

        return InstrumentedProcessHelper.Start(instrumentationScriptPath, args, EnvironmentHelper);
    }
}
