// <copyright file="MySqlDataTests.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MySqlCollection.Name)]
public class MySqlDataTests : TestHelper
{
    private readonly MySqlFixture _mySql;

    public MySqlDataTests(ITestOutputHelper output, MySqlFixture mySql)
        : base("MySqlData", output)
    {
        _mySql = mySql;
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.MySqlData");

        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1"); // uses bytecode instrumentation
        RunTestApplication(new()
        {
            Arguments = $"--mysql {_mySql.Port}"
        });

        collector.AssertExpectations();
    }
}
#endif
