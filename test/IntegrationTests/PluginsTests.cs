// <copyright file="PluginsTests.cs" company="OpenTelemetry Authors">
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

using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class PluginsTests : TestHelper
{
    public PluginsTests(ITestOutputHelper output)
        : base("Plugins", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await MockSpansCollector.Start(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitMetrics()
    {
        using var collector = await MockMetricsCollector.Start(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        RunTestApplication();

        collector.AssertExpectations();
    }
}
