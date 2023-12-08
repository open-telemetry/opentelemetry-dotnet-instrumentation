// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
        var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(new List<ResourceDetector>());
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

            var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(Array.Empty<ResourceDetector>());
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
