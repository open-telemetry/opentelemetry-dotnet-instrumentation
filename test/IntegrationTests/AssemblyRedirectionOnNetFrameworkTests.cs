// <copyright file="AssemblyRedirectionOnNetFrameworkTests.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AssemblyRedirectionOnNetFrameworkTests : TestHelper
{
    public AssemblyRedirectionOnNetFrameworkTests(ITestOutputHelper output)
        : base("AssemblyRedirection.NetFramework", output)
    {
    }

    [Fact]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        const string TestApplicationActivitySource = "AssemblyRedirection.NetFramework.ActivitySource";
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", TestApplicationActivitySource);
        collector.Expect(TestApplicationActivitySource);

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
