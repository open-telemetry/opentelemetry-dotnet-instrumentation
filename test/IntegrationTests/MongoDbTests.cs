// <copyright file="MongoDbTests.cs" company="OpenTelemetry Authors">
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
#if NETCOREAPP
using System.Collections.Generic;
#endif
using System.Linq;
using System.Text.RegularExpressions;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.MongoDB
{
    [Collection(MongoDbCollection.Name)]
    public class MongoDbTests : TestHelper
    {
        private readonly MongoDbFixture _mongoDb;

        public MongoDbTests(ITestOutputHelper output, MongoDbFixture mongoDb)
            : base("MongoDB", output)
        {
            _mongoDb = mongoDb;

            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "MongoDb");
            SetEnvironmentVariable("OTEL_SERVICE_NAME", "TestApplication.MongoDB");
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("Containers", "Linux")]
        public void SubmitsTraces()
        {
            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(Output, agentPort))
            using (var processResult = RunTestApplicationAndWaitForExit(agent.Port, arguments: $"--mongo-db {_mongoDb.Port}", enableStartupHook: true))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(3, TimeSpan.FromSeconds(5));
                Assert.True(spans.Count >= 3, $"Expecting at least 3 spans, only received {spans.Count}");

                var rootSpan = spans.Single(s => s.ParentId == null);

                // Check for manual trace
                Assert.Equal("Main()", rootSpan.Name);
                Assert.Null(rootSpan.Type);

                int spansWithStatement = 0;

                foreach (var span in spans)
                {
                    Assert.Equal("TestApplication.MongoDB", span.Service);

                    if (Regex.IsMatch(span.Name, "employees\\.*"))
                    {
                        Assert.Equal("mongodb", span.Tags["db.system"]);
                        Assert.Equal("test-db", span.Tags["db.name"]);
                        Assert.True("1.0.0.0" == span.Tags["otel.library.version"], span.ToString());

                        if (span.Tags?.ContainsKey("db.statement") ?? false)
                        {
                            spansWithStatement++;
                            Assert.True(span.Tags?.ContainsKey("db.statement"), $"No db.statement found on span {span}");
                        }
                    }
                    else
                    {
                        // These are manual (DiagnosticSource) traces
                        Assert.True("1.0.0" == span.Tags["otel.library.version"], span.ToString());
                    }
                }

                Assert.False(spansWithStatement == 0, "Extraction of the command failed on all spans");
            }
        }
    }
}
