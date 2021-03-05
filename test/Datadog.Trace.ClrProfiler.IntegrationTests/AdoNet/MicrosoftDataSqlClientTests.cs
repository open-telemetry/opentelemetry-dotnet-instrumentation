#if !NET452
using System.Collections.Generic;
using System.Linq;
using Datadog.Core.Tools;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class MicrosoftDataSqlClientTests : TestHelper
    {
        public MicrosoftDataSqlClientTests(ITestOutputHelper output)
            : base("Microsoft.Data.SqlClient", output)
        {
            SetServiceVersion("1.0.0");
        }

        public static IEnumerable<object[]> GetMicrosoftDataSqlClient()
        {
            foreach (object[] item in PackageVersions.MicrosoftDataSqlClient)
            {
                yield return item.Concat(new object[] { false, false, }).ToArray();
                yield return item.Concat(new object[] { true, false, }).ToArray();
                yield return item.Concat(new object[] { true, true, }).ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(GetMicrosoftDataSqlClient))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitsTracesWithNetStandard(string packageVersion, bool enableCallTarget, bool enableInlining)
        {
            SetCallTargetSettings(enableCallTarget, enableInlining);

            // ALWAYS: 133 spans
            // - SqlCommand: 21 spans (3 groups * 7 spans)
            // - DbCommand:  42 spans (6 groups * 7 spans)
            // - IDbCommand: 14 spans (2 groups * 7 spans)
            // - DbCommand-netstandard:  42 spans (6 groups * 7 spans)
            // - IDbCommand-netstandard: 14 spans (2 groups * 7 spans)
            //
            // CALLSITE: +4 spans
            // - IDbCommandGenericConstrant<T>: 4 spans (2 group * 2 spans)
            //
            // CALLTARGET: +14 spans
            // - IDbCommandGenericConstrant<SqlCommand>: 7 spans (1 group * 7 spans)
            // - IDbCommandGenericConstrant<SqlCommand>-netstandard: 7 spans (1 group * 7 spans)
#if NET461
            var expectedSpanCount = 133;
#else
            var expectedSpanCount = 137;
#endif

            if (enableCallTarget)
            {
                expectedSpanCount = 147;
            }

            const string dbType = "sql-server";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Microsoft.Data.SqlClient-" + dbType;

            // NOTE: opt into the additional instrumentation of calls into netstandard.dll
            SetEnvironmentVariable("OTEL_TRACE_NETSTANDARD_ENABLED", "true");

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                int actualSpanCount = spans.Where(s => s.ParentId.HasValue).Count(); // Remove unexpected DB spans from the calculation
                Assert.Equal(expectedSpanCount, actualSpanCount);

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

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SpansDisabledByAdoNetExcludedTypes(bool enableCallTarget, bool enableInlining)
        {
            SetCallTargetSettings(enableCallTarget, enableInlining);

            var totalSpanCount = 21;

            const string dbType = "sql-server";
            const string expectedOperationName = dbType + ".query";

            SetEnvironmentVariable(ConfigurationKeys.AdoNetExcludedTypes, "System.Data.SqlClient.SqlCommand;Microsoft.Data.SqlClient.SqlCommand;MySql.Data.MySqlClient.MySqlCommand;Npgsql.NpgsqlCommand");

            string packageVersion = PackageVersions.MicrosoftDataSqlClient.First()[0] as string;
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
#endif
