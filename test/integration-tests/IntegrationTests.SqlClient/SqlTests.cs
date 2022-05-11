// <copyright file="SqlTests.cs" company="OpenTelemetry Authors">
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
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.SqlClient
{
    [Collection(SqlClientCollection.Name)]
    public class SqlTests : TestHelper
    {
        private const string ServiceName = "TestApplication.SqlClient";

        public SqlTests(ITestOutputHelper output)
            : base("SqlClient", output)
        {
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "SqlClient");
        }

        [Fact]
        public void SubmitTraces()
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockZipkinCollector(Output, agentPort);

            const int expectedSpanCount = 1;

            using var processResult = RunTestApplicationAndWaitForExit(agent.Port, enableStartupHook: true);
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
            var spans = agent.WaitForSpans(expectedSpanCount, TimeSpan.FromSeconds(5));

            using (new AssertionScope())
            {
                spans.Count.Should().Be(expectedSpanCount);
            }
        }
    }
}
