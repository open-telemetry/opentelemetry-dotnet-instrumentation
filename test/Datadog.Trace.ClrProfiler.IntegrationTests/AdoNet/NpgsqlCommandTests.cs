using System.Linq;
using Datadog.Core.Tools;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class NpgsqlCommandTests : TestHelper
    {
        public NpgsqlCommandTests(ITestOutputHelper output)
            : base("Npgsql", output)
        {
            SetServiceVersion("1.0.0");
        }

        [Theory]
        [MemberData(nameof(PackageVersions.Npgsql), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTracesWithNetStandard(string packageVersion)
        {
            // Note: The automatic instrumentation currently does not instrument on the generic wrappers
            // due to an issue with constrained virtual method calls. This leads to an inconsistency where
            // a newer library version may have 4 more spans than an older one (2 ExecuteReader calls *
            // 2 interfaces: IDbCommand and IDbCommand-netstandard).
            // Once this is fully supported, this will add another 2 complete groups instead.
#if NET452
            var expectedSpanCount = 50; // 7 queries * 7 groups + 1 internal query
#else
            var expectedSpanCount = 78; // 7 queries * 11 groups + 1 internal query
#endif

            const string dbType = "postgres";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Npgsql-" + dbType;

            // NOTE: opt into the additional instrumentation of calls into netstandard.dll
            // see https://github.com/DataDog/dd-trace-dotnet/pull/753
            SetEnvironmentVariable("DD_TRACE_NETSTANDARD_ENABLED", "true");

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                // Assert.Equal(expectedSpanCount, spans.Count); // Assert an exact match once we can correctly instrument the generic constraint case
                Assert.True(spans.Count == expectedSpanCount || spans.Count == expectedSpanCount + 4);

                foreach (var span in spans)
                {
                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal(SpanTypes.Sql, span.Type);
                    Assert.Equal(dbType, span.Tags[Tags.DbType]);
                    Assert.False(span.Tags?.ContainsKey(Tags.Version), "External service span should not have service version tag.");
                }
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SpansDisabledByAdoNetExcludedTypes()
        {
            var totalSpanCount = 21;

            const string dbType = "postgres";
            const string expectedOperationName = dbType + ".query";

            SetEnvironmentVariable(ConfigurationKeys.AdoNetExcludedTypes, "System.Data.SqlClient.SqlCommand;Microsoft.Data.SqlClient.SqlCommand;MySql.Data.MySqlClient.MySqlCommand;Npgsql.NpgsqlCommand");

            string packageVersion = PackageVersions.Npgsql.First()[0] as string;
            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(totalSpanCount, returnAllOperations: true);
                Assert.NotEmpty(spans);
                Assert.Empty(spans.Where(s => s.Name.Equals(expectedOperationName)));
            }
        }
    }
}
