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

using System.IO;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Newtonsoft.Json;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[UsesVerify]
public class ModuleTests : TestHelper
{
    public ModuleTests(ITestOutputHelper output)
        : base("Modules", output)
    {
    }

    [Fact]
    public async Task RunApplication()
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            RunTestApplication(arguments: $"--temp-path {tempPath}");

            if (!File.Exists(tempPath))
            {
                Assert.Fail("Could not find modules report file.");
            }

            var json = File.ReadAllText(tempPath);
            var modules = JsonConvert.DeserializeObject<string[]>(json);

            await Verifier.Verify(modules)
                 .UniqueForOSPlatform()
                 .UniqueForRuntime()
                 .DisableDiff();
        }
        finally
        {
            // Cleanup
            File.Delete(tempPath);
        }
    }
}
