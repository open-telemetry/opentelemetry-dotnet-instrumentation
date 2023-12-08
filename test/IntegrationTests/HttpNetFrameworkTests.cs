// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class HttpNetFrameworkTests : TestHelper
{
    public HttpNetFrameworkTests(ITestOutputHelper output)
        : base("Http.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
