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
        var seenPropagators = new HashSet<string>();
        var resolvedPropagators = new List<string>();
        var propagators = new List<Propagator>();

        if (Composite != null)
        {
            foreach (var key in Composite.Keys)
            {
                if (seenPropagators.Add(key))
                {
                    resolvedPropagators.Add(key);
                }
            }
        }

        if (!string.IsNullOrEmpty(CompositeList))
        {
            var list = CompositeList!.Split(Constants.ConfigurationValues.Separator);
            foreach (var item in list)
            {
                if (seenPropagators.Add(item))
                {
                    resolvedPropagators.Add(item);
                }
            }
        }

        foreach (var propagatorName in resolvedPropagators)
        {
            switch (propagatorName.ToLowerInvariant())
            {
                case Constants.ConfigurationValues.Propagators.W3CTraceContext:
                    propagators.Add(Propagator.W3CTraceContext);
                    break;
                case Constants.ConfigurationValues.Propagators.W3CBaggage:
                    propagators.Add(Propagator.W3CBaggage);
                    break;
                case Constants.ConfigurationValues.Propagators.B3Multi:
                    propagators.Add(Propagator.B3Multi);
                    break;
                case Constants.ConfigurationValues.Propagators.B3Single:
                    propagators.Add(Propagator.B3Single);
                    break;
            }
        }

        return propagators;
    }
}
