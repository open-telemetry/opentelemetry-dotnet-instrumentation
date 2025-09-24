// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class ServiceNameConfiguratorTests
{
    private const string ServiceName = "service.name";
    private const string OtelServiceVariable = "OTEL_SERVICE_NAME";

    [Fact]
    public void GetFallbackServiceName()
    {
        var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(new ResourceSettings());
        var resource = resourceBuilder.Build();

        var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value as string;
        Assert.Matches("testhost|ReSharperTestRunner", serviceName);
    }

    [Fact]
    public void ServiceName_Retained_EnvVarSet()
    {
        const string setServiceName = "TestApplication";
        try
        {
            Environment.SetEnvironmentVariable(OtelServiceVariable, setServiceName);

            var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(new ResourceSettings());
            var resource = resourceBuilder.Build();

            var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value as string;

            Assert.Equal(setServiceName, serviceName);
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtelServiceVariable, null);
        }
    }
}
