// <copyright file="ModuleTests.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[UsesVerify]
public class ModuleTests : TestHelper
{
    public ModuleTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
    }

    [Fact]
    public async Task RunApplication()
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");

        var process = StartTestApplication();

        ProcessModuleCollection collection = null;
        var hasMatchedOnce = false;
        var stabilizations = 0;

        // Process info stabilizer
        while (true)
        {
            await Task.Delay(2000);

            ProcessModuleCollection currentModules = process.Modules;

            if (currentModules.Count > 0)
            {
                if (collection != null && collection.Count == currentModules.Count && !hasMatchedOnce)
                {
                    hasMatchedOnce = true;
                }

                if (collection != null && hasMatchedOnce)
                {
                    break;
                }

                collection = currentModules;
            }

            // fail if stabilization is longer than 40s
            if (stabilizations++ > 20)
            {
                throw new TimeoutException("Could not stabilize process");
            }
        }

        var modules = process.Modules
            .OfType<ProcessModule>()
            .Select(x => x.ModuleName)
            .Where(name => name.StartsWith("OpenTelemetry"))
            .OrderBy(name => name)
            .ToList();

        if (!process.HasExited)
        {
            process.Kill();
        }

        await Verifier.Verify(modules)
            .UseDirectory("./snapshots")
            .DisableDiff();
    }
}
#endif
