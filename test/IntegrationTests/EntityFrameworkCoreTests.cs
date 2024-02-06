// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class EntityFrameworkCoreTests : TestHelper
{
    public EntityFrameworkCoreTests(ITestOutputHelper output)
        : base("EntityFrameworkCore", output)
    {
    }

    public static IEnumerable<object[]> GetData()
    {
        foreach (var version in LibraryVersion.EntityFrameworkCore)
        {
            yield return new[] { version[0], true };
            yield return new[] { version[0], false };
        }
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ENTITYFRAMEWORKCORE_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.EntityFrameworkCore", span => Expect(span, dbStatementForText));

        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }

    private static bool Expect(Span span, bool dbStatementForText)
    {
        if (dbStatementForText && !span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)))
        {
            return false;
        }

        if (!dbStatementForText && span.Attributes.Any(attr => attr.Key == "db.statement"))
        {
            return false;
        }

        return true;
    }
}
#endif
