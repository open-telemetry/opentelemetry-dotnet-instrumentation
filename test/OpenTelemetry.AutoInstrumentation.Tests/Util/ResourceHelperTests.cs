// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

public class ResourceHelperTests
{
    [Fact]
    public void AggregateResources_AllProvidersNull_ReturnsEmptyResource()
    {
        var attributes1 = new Dictionary<string, object>
        {
            { "service.name", "my-service" },
            { "service.version", "1.0.0" }
        };

        var attributes2 = new Dictionary<string, object>
        {
            { "service.name", "my-service-2" },
            { "service.namespace", "my-namespace" }
        };

        var tracerProvider = Sdk
            .CreateTracerProviderBuilder()
            .ConfigureResource(resource =>
            {
                resource.Clear();
                resource.AddAttributes(attributes1);
            })
            .Build();

        var meterProvider = Sdk
            .CreateMeterProviderBuilder()
            .ConfigureResource(resource =>
            {
                resource.Clear();
                resource.AddAttributes(attributes2);
            })
            .Build();

        var resource = ResourceHelper.AggregateResources(tracerProvider, meterProvider);

        Assert.Collection(
            resource.Attributes,
            attribute =>
            {
                Assert.Equal("service.name", attribute.Key);
                Assert.Equal("my-service-2", attribute.Value);
            },
            attribute =>
            {
                Assert.Equal("service.namespace", attribute.Key);
                Assert.Equal("my-namespace", attribute.Value);
            },
            attribute =>
            {
                Assert.Equal("service.version", attribute.Key);
                Assert.Equal("1.0.0", attribute.Value);
            });
    }
}
