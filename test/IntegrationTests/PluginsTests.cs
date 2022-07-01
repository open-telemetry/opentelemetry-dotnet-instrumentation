// <copyright file="PluginsTests.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class PluginsTests : TestHelper
{
    public PluginsTests(ITestOutputHelper output)
        : base("Plugins", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        using var collector = new MockZipkinCollector(Output);
        RunTestApplication(collector.Port);
        var spans = collector.WaitForSpans(1, TimeSpan.FromSeconds(5));

        spans.Should().Contain(x => x.Name == "SayHello");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        using var collector = new MockMetricsCollector(Output);
        RunTestApplication(metricsAgentPort: collector.Port);
        var metricRequests = collector.WaitForMetrics(1, TimeSpan.FromSeconds(5));

        var metrics = metricRequests.Should().NotBeEmpty().And.Subject.First().ResourceMetrics.Should().ContainSingle().Subject.ScopeMetrics;
        metrics.Should().Contain(x => x.Scope.Name == "MyCompany.MyProduct.MyLibrary");
    }
}
