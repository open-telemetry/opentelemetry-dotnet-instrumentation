// <copyright file="SqlClientTests.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    [Collection(SqlClientCollection.Name)]
    public class SqlClientTests : TestHelper
    {
        private const string ServiceName = "TestApplication.SqlClient";
        private readonly SqlClientFixture _sqlClientFixture;

        public SqlClientTests(ITestOutputHelper output, SqlClientFixture sqlClientFixture)
            : base("SqlClient", output)
        {
            _sqlClientFixture = sqlClientFixture;
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("Containers", "Linux")]
        public void SubmitTraces()
        {
            using var agent = new MockZipkinCollector(Output);

            const int expectedSpanCount = 8;

            RunTestApplication(agent.Port, arguments: $"{_sqlClientFixture.Password} {_sqlClientFixture.Port}");
            var spans = agent.WaitForSpans(expectedSpanCount, TimeSpan.FromSeconds(5));

            using (new AssertionScope())
            {
                spans.Count.Should().Be(expectedSpanCount);

                foreach (var span in spans)
                {
                    span.Service.Should().Be(ServiceName);
                    span.Name.Should().Be("master");
                    span.Tags.Should().Contain(new KeyValuePair<string, string>("db.system", "mssql"));
                    span.Tags.Should().Contain(new KeyValuePair<string, string>("db.name", "master"));
                    span.Tags.Should().Contain(new KeyValuePair<string, string>("peer.service", $"localhost,{_sqlClientFixture.Port}"));
                    span.Tags.Should().Contain(new KeyValuePair<string, string>("span.kind", "client"));
                }
            }
        }
    }
}
