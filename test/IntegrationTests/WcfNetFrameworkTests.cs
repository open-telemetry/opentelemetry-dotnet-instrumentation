// <copyright file="WcfNetFrameworkTests.cs" company="OpenTelemetry Authors">
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

#if NET462
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfNetFrameworkTests : WcfTestsBase
{
    private const string ServiceName = "TestApplication.Client.Server.NetFramework";

    public WcfNetFrameworkTests(ITestOutputHelper output)
        : base(ServiceName, output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task TracesResource()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName); // this is set via env var and App.config, but env var has precedence
        // collector.ResourceExpector.Expect("deployment.environment", "test"); // this is set via App.config

        var serverHelper = new WcfServerTestHelper(Output);
        ServerProcess = serverHelper.RunWcfServer(collector);
        await WaitForServer();

        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }
}

#endif
