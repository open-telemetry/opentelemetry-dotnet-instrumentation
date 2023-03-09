// <copyright file="ServiceNameConfiguratorTests.cs" company="OpenTelemetry Authors">
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

using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class ServiceNameConfiguratorTests
{
    private const string ServiceName = "service.name";
    private const string OtelServiceVariable = "OTEL_SERVICE_NAME";

    [Fact]
    public void GetFallbackServiceName()
    {
        var resourceBuilder = ResourceConfigurator.CreateResourceBuilder();
        var resource = resourceBuilder.Build();

        var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value as string;
        serviceName?.Should().Be("testhost");
    }

    [Fact]
    public void ServiceName_Retained_EnvVarSet()
    {
        const string setServiceName = "TestApplication";
        try
        {
            Environment.SetEnvironmentVariable(OtelServiceVariable, setServiceName);

            var resourceBuilder = ResourceConfigurator.CreateResourceBuilder();
            var resource = resourceBuilder.Build();

            var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value as string;

            serviceName?.Should().Be(setServiceName);
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtelServiceVariable, null);
        }
    }
}
