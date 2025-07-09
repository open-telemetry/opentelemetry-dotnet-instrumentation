// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using Xunit.Sdk;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedSdkSettingsTests
{
    [Fact]
    public void Loads_AttributeLimits()
    {
        var config = new Conf
        {
            AttributeLimits = new AttributeLimits
            {
                AttributeCountLimit = 100,
                AttributeValueLengthLimit = 256,
            }
        };

        var settings = new SdkSettings();

        settings.LoadFile(config);

        Assert.Equal(100, settings.AttributeLimits.AttributeCountLimit);
        Assert.Equal(256, settings.AttributeLimits.AttributeValueLengthLimit);
    }

    [Fact]
    public void Returns_When_Propagator_IsNull()
    {
        var config = new Conf { Propagator = null };
        var settings = new SdkSettings();

        settings.LoadFile(config);

        Assert.Empty(settings.Propagators);
    }

    [Fact]
    public void Adds_Unique_Propagators_From_Composite()
    {
        var config = new Conf
        {
            Propagator = new PropagatorConfiguration
            {
                Composite = new Dictionary<string, object>
                {
                    { Constants.ConfigurationValues.Propagators.W3CTraceContext, new object() },
                    { Constants.ConfigurationValues.Propagators.W3CBaggage, new object() }
                }
            }
        };
        var settings = new SdkSettings();

        settings.LoadFile(config);

        Assert.Contains(Propagator.W3CTraceContext, settings.Propagators);
        Assert.Contains(Propagator.W3CBaggage, settings.Propagators);
    }

    [Fact]
    public void Adds_Propagators_From_CompositeList()
    {
        var config = new Conf
        {
            Propagator = new PropagatorConfiguration
            {
                CompositeList = "tracecontext,baggage"
            }
        };
        var settings = new SdkSettings();

        settings.LoadFile(config);

        Assert.Contains(Propagator.W3CTraceContext, settings.Propagators);
        Assert.Contains(Propagator.W3CBaggage, settings.Propagators);
    }

    [Fact]
    public void Ignores_Duplicates_From_Composite_And_CompositeList()
    {
        var config = new Conf
        {
            Propagator = new PropagatorConfiguration
            {
                Composite = new Dictionary<string, object>
                {
                    { "tracecontext", new object() }
                },
                CompositeList = "tracecontext,baggage"
            }
        };
        var settings = new SdkSettings();

        settings.LoadFile(config);

        Assert.Equal(2, settings.Propagators.Count);
    }

    [Fact]
    public void Logs_And_Skips_Unknown_Propagator()
    {
        var config = new Conf
        {
            Propagator = new PropagatorConfiguration
            {
                CompositeList = "custom"
            }
        };
        var settings = new SdkSettings();

        settings.LoadFile(config);
    }
}
