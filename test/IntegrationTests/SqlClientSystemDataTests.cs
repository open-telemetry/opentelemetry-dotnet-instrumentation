// <copyright file="SqlClientSystemDataTests.cs" company="OpenTelemetry Authors">
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

using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class SqlClientSystemDataTests : TestHelper
{
    private readonly SqlServerFixture _sqlServerFixture;

    public SqlClientSystemDataTests(ITestOutputHelper output, SqlServerFixture sqlServerFixture)
        : base("SqlClient.NetFramework", output)
    {
        _sqlServerFixture = sqlServerFixture;
    }

    [IgnoreRunningOnNet481Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.SqlClient");

        RunTestApplication(new()
        {
            Arguments = $"{_sqlServerFixture.Password} {_sqlServerFixture.Port}"
        });

        collector.AssertExpectations();
    }
}

public sealed class IgnoreRunningOnNet481Fact : FactAttribute
{
    public IgnoreRunningOnNet481Fact()
    {
        var netVersion = RuntimeHelper.GetRuntimeVersion();
        if (netVersion == "4.8.1+")
        {
            // https://github.com/open-telemetry/opentelemetry-dotnet/issues/3901
            Skip = "NET Framework 4.8.1 is skipped due bug.";
        }
    }
}

#endif
