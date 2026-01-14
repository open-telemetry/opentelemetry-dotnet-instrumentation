// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public abstract class WcfServerTestHelperBase : TestHelper
{
    protected WcfServerTestHelperBase(string testApplicationName, ITestOutputHelper output, string serviceName)
        : base(testApplicationName, output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", serviceName);
    }

    internal abstract string ServerInstrumentationScopeName { get; }

    internal abstract ProcessHelper RunWcfServer(MockSpansCollector collector);
}
