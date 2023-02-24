// <copyright file="ServiceNameDetectorTests.cs" company="OpenTelemetry Authors">
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

using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class ServiceNameDetectorTests
{
    [Fact]
    public void Detector_ReturnsTestHost()
    {
        var expectedServiceName = Assembly.GetEntryAssembly()?.GetName().Name;
        var settings = Settings.FromDefaultSources<GeneralSettings>();

        ServiceNameDetector serviceNameDetector = new();
        Resource resource = serviceNameDetector.Detect();

        resource.Attributes.Single(a => a.Key == "service.name").Value.Should().Be(expectedServiceName);
    }
}
