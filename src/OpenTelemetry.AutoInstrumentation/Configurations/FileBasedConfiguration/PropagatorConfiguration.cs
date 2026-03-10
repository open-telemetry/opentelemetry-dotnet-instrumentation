// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class PropagatorConfiguration
{
    /// <summary>
    /// Gets or sets the composite propagator configuration.
    /// </summary>
    [YamlMember(Alias = "composite")]
    public Dictionary<string, object>? Composite { get; set; }

    /// <summary>
    /// Gets or sets the composite list for the propagator.
    /// </summary>
    [YamlMember(Alias = "composite_list")]
    public string? CompositeList { get; set; }

    public IReadOnlyList<Propagator> GetEnabledPropagators()
    {
        if (Composite is null && string.IsNullOrEmpty(CompositeList))
        {
            return [];
        }

        var propagatorNames = (Composite?.Keys ?? Enumerable.Empty<string>())
            .Concat(!string.IsNullOrEmpty(CompositeList)
                    ? CompositeList!.Split(Constants.ConfigurationValues.Separator)
                    : [])
            .Distinct();

        var propagators = propagatorNames
            .Select<string, Propagator?>(name => name switch
            {
                Constants.ConfigurationValues.Propagators.W3CTraceContext => Propagator.W3CTraceContext,
                Constants.ConfigurationValues.Propagators.W3CBaggage => Propagator.W3CBaggage,
                Constants.ConfigurationValues.Propagators.B3Multi => Propagator.B3Multi,
                Constants.ConfigurationValues.Propagators.B3Single => Propagator.B3Single,
                _ => null
            })
            .OfType<Propagator>()
            .ToList();

        return propagators;
    }
}
