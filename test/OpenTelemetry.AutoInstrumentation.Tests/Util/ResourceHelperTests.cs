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
    public void ResourceHelper_AggregateResources()
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

        // Convert attributes to a dictionary to ensure order-independent assertions
        var actualAttributes = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.Equal(3, actualAttributes.Count);
        Assert.Equal("my-service-2", actualAttributes["service.name"]);
        Assert.Equal("my-namespace", actualAttributes["service.namespace"]);
        Assert.Equal("1.0.0", actualAttributes["service.version"]);
    }
}
