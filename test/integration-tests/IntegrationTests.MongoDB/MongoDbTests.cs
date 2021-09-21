#if NETCOREAPP
using System.Collections.Generic;
#endif

using System.Linq;
using IntegrationTests.Helpers;
using OpenTelemetry.ClrProfiler.Managed.Tagging;
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
            SetServiceVersion("1.0.0");

            _mongoDb = mongoDb;
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

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
                Assert.Equal("Samples.MongoDB", rootSpan.Service);
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
                        Assert.Equal("Samples.MongoDB", span.Service);
                        Assert.True("1.0.0" == span.Tags?.GetValueOrDefault("otel.library.version"), span.ToString());
                    }
                }

                Assert.False(spansWithResourceName == 0, "Extraction of the command failed on all spans");
            }
        }
    }
}
