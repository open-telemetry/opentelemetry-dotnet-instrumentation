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
        private readonly SqlClientFixture _sqlClientFixture;

        public SqlTests(ITestOutputHelper output, SqlClientFixture sqlClientFixture)
            : base("SqlClient", output)
        {
            _sqlClientFixture = sqlClientFixture;
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "SqlClient");
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("Containers", "Linux")]
        public void SubmitTraces()
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockZipkinCollector(Output, agentPort);

            const int expectedSpanCount = 8;

            using var processResult = RunTestApplicationAndWaitForExit(agent.Port, arguments: $"--database-port {_sqlClientFixture.Port}", enableStartupHook: true);
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
            var spans = agent.WaitForSpans(expectedSpanCount, TimeSpan.FromSeconds(5));

            using (new AssertionScope())
            {
                spans.Count.Should().Be(expectedSpanCount);

                foreach (var span in spans)
                {
                    span.Service.Should().Be(ServiceName);
                    span.Name.Should().Be("master");
                    span.Tags["db.system"].Should().Be("mssql");
                    span.Tags["db.name"].Should().Be("master");
                    span.Tags["peer.service"].Should().Contain($"{_sqlClientFixture.Port}");
                    span.Tags["db.statement_type"].Should().Be("Text");
                    span.Tags["span.kind"].Should().Be("client");
                }
            }
        }
    }
}
