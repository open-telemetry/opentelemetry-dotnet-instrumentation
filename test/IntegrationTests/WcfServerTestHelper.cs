// <copyright file="WcfServerTestHelper.cs" company="OpenTelemetry Authors">
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
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

internal class WcfServerTestHelper : TestHelper
{
    private const string ServiceName = "TestApplication.Wcf.Server.NetFramework";

    public WcfServerTestHelper(ITestOutputHelper output)
        : base("Wcf.Server.NetFramework", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    public ProcessHelper RunWcfServer(MockSpansCollector collector)
    {
        var projectDirectory = EnvironmentHelper.GetTestApplicationProjectDirectory();
        var exeFileName = $"{EnvironmentHelper.FullTestApplicationName}.exe";
        var testApplicationPath = Path.Combine(projectDirectory, "bin", EnvironmentTools.GetPlatform().ToLowerInvariant(), EnvironmentTools.GetBuildConfiguration(), "net462", exeFileName);

        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"Unable to find executing assembly at {testApplicationPath}");
        }

        SetExporter(collector);
        var process = InstrumentedProcessHelper.StartInstrumentedProcess(testApplicationPath, EnvironmentHelper, null);
        return new ProcessHelper(process);
    }
}
