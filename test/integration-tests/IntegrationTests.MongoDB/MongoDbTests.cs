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

#if NETCOREAPP
using System.Collections.Generic;
#endif

using System.Linq;
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Tagging;
using Xunit;
using Xunit.Abstractions;

#if NETFRAMEWORK
using IntegrationTests.Helpers.Comptability;
#endif

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
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            const string ServiceName = "Samples.MongoDB";
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(Output, agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"--mongo-db {_mongoDb.Port}"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(3, 500);
                Assert.True(spans.Count >= 3, $"Expecting at least 3 spans, only received {spans.Count}");

                var rootSpan = spans.Single(s => s.ParentId == null);

                // Check for manual trace
                Assert.Equal("Main()", rootSpan.Name);
                Assert.Equal(ServiceName, rootSpan.Service);
                Assert.Null(rootSpan.Type);

                int spansWithResourceName = 0;

                foreach (var span in spans)
                {
                    if (span == rootSpan)
                    {
                        continue;
                    }

                    if (span.Name == "mongodb.query")
                    {
                        Assert.Equal("mongodb.query", span.Name);
                        Assert.Equal("MongoDb", span.Type);
                        Assert.True("0.0.1" == span.Tags?.GetValueOrDefault("otel.library.version"), span.ToString());

                        if (span.Resource != null && span.Resource != "mongodb.query")
                        {
                            spansWithResourceName++;
                            Assert.True(span.Tags?.ContainsKey(Tags.MongoDb.Query), $"No query found on span {span}");
                        }
                    }
                    else
                    {
                        // These are manual (DiagnosticSource) traces
                        Assert.True("1.0.0" == span.Tags?.GetValueOrDefault("otel.library.version"), span.ToString());
                    }

                    Assert.Equal(ServiceName, span.Service);
                }

                Assert.False(spansWithResourceName == 0, "Extraction of the command failed on all spans");
            }
        }
    }
}
