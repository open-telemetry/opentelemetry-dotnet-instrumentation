// <copyright file="ElasticsearchTests.cs" company="OpenTelemetry Authors">
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

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class ElasticsearchTests : TestHelper
{
    public ElasticsearchTests(ITestOutputHelper output)
        : base("Elasticsearch", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Elasticsearch), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        if (string.IsNullOrEmpty(packageVersion) || new Version(packageVersion).CompareTo(new Version(8, 10, 0)) >= 0)
        {
            collector.Expect("Elastic.Transport");
        }
        else
        {
            collector.Expect("Elastic.Clients.Elasticsearch.ElasticsearchClient");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }
}
