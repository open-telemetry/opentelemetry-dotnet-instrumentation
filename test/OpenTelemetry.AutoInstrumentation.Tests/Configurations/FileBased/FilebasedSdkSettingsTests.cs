// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedSdkSettingsTests
{
    [Fact]
    public void LoadFile_CompositeOnlyPropagators()
    {
        var propagator = new PropagatorConfiguration
        {
            Composite = new Dictionary<string, object>
            {
                { "tracecontext", new object() },
                { "baggage", new object() }
            }
        };

        var conf = new YamlConfiguration { Propagator = propagator };
        var settings = new SdkSettings();

        settings.LoadFile(conf);

        Assert.Equal(2, settings.Propagators.Count);
        Assert.Contains(Propagator.W3CTraceContext, settings.Propagators);
        Assert.Contains(Propagator.W3CBaggage, settings.Propagators);
    }

    [Fact]
    public void LoadFile_CompositeListOnlyPropagators()
    {
        var propagator = new PropagatorConfiguration
        {
            CompositeList = "b3multi,b3"
        };

        var conf = new YamlConfiguration { Propagator = propagator };
        var settings = new SdkSettings();

        settings.LoadFile(conf);

        Assert.Equal(2, settings.Propagators.Count);
        Assert.Contains(Propagator.B3Multi, settings.Propagators);
        Assert.Contains(Propagator.B3Single, settings.Propagators);
    }

    [Fact]
    public void LoadFile_CompositeAndCompositeList_AreMergedWithoutDuplicates()
    {
        var propagator = new PropagatorConfiguration
        {
            Composite = new Dictionary<string, object>
            {
                { "tracecontext", new object() },
                { "baggage", new object() }
            },
            CompositeList = "tracecontext,b3"
        };

        var conf = new YamlConfiguration { Propagator = propagator };
        var settings = new SdkSettings();

        settings.LoadFile(conf);

        Assert.Equal(3, settings.Propagators.Count);
        Assert.Contains(Propagator.W3CTraceContext, settings.Propagators);
        Assert.Contains(Propagator.W3CBaggage, settings.Propagators);
        Assert.Contains(Propagator.B3Single, settings.Propagators);
    }

    [Fact]
    public void LoadFile_GetEnabledPropagators_Empty()
    {
        var propagator = new PropagatorConfiguration();

        var conf = new YamlConfiguration { Propagator = propagator };
        var settings = new SdkSettings();

        settings.LoadFile(conf);

        Assert.Empty(settings.Propagators);
    }

    [Fact]
    public void LoadFile_GetEnabledPropagators_UnknownValues_AreIgnored()
    {
        var propagator = new PropagatorConfiguration
        {
            CompositeList = "invalid,b3multi"
        };

        var conf = new YamlConfiguration { Propagator = propagator };
        var settings = new SdkSettings();

        settings.LoadFile(conf);

        Assert.Single(settings.Propagators);
        Assert.Equal(Propagator.B3Multi, settings.Propagators[0]);
    }
}
