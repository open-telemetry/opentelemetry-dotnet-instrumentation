// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class ServiceNameConfiguratorTests
{
    private const string ServiceInstanceId = "service.instance.id";
    private const string ServiceName = "service.name";
    private const string OtelServiceVariable = "OTEL_SERVICE_NAME";
    private const string OtelResourceAttributeVariable = "OTEL_RESOURCE_ATTRIBUTES";

    [Fact]
    public void GetFallbackServiceName()
    {
        var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(new ResourceSettings());
        var resource = resourceBuilder.Build();

        var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value as string;
        Assert.Matches("testhost|ReSharperTestRunner", serviceName);
    }

    [Theory]
    [InlineData(ServiceName, OtelServiceVariable, "TestApplication", "TestApplication")]
    [InlineData(ServiceName, OtelResourceAttributeVariable, $"{ServiceName}=TestApplication", "TestApplication")]
    [InlineData(ServiceInstanceId, OtelResourceAttributeVariable, $"{ServiceInstanceId}=c8de43ab-0121-4a4a-84ff-2177e1613304", "c8de43ab-0121-4a4a-84ff-2177e1613304")]
    public void Environment_Variable_Not_Overwritten(
        string attributeName,
        string variableName,
        string variableValue,
        string expectedValue)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, variableValue);

            var resourceBuilder = ResourceConfigurator.CreateResourceBuilder(new ResourceSettings());
            var resource = resourceBuilder.Build();

            var actualValue = resource.Attributes.FirstOrDefault(a => a.Key == attributeName).Value as string;

            Assert.Equal(expectedValue, actualValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
