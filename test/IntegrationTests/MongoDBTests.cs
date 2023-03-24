// <copyright file="MongoDBTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER
using IntegrationTests.Helpers;
#if NUGET_PACKAGE_TESTS
using NuGetPackagesTests;
#endif
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MongoDBCollection.Name)]
public class MongoDBTests
#if NUGET_PACKAGE_TESTS
    : NugetTestHelper
#else
    : TestHelper
#endif
{
    private readonly MongoDBFixture _mongoDB;

    public MongoDBTests(ITestOutputHelper output, MongoDBFixture mongoDB)
#if NUGET_PACKAGE_TESTS
        : base("MongoDB.Nuget", output)
#else
        : base("MongoDB", output)
#endif
    {
        _mongoDB = mongoDB;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
#if NUGET_PACKAGE_TESTS
    [InlineData("")]
#else
    [MemberData(nameof(LibraryVersion.MongoDB), MemberType = typeof(LibraryVersion))]
#endif
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        const int spanCount = 3;
        for (int i = 0; i < spanCount; i++)
        {
            collector.Expect("MongoDB.Driver.Core.Extensions.DiagnosticSources");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--mongo-db {_mongoDB.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
#endif
