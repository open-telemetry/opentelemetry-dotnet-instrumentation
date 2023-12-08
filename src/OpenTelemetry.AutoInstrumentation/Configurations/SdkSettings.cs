// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Propagator Settings
/// </summary>
internal class SdkSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets the list of propagators to be used.
    /// </summary>
    public IList<Propagator> Propagators { get; } = new List<Propagator>();

    protected override void OnLoad(Configuration configuration)
    {
        var propagators = configuration.GetString(ConfigurationKeys.Sdk.Propagators);

        if (!string.IsNullOrEmpty(propagators))
        {
            foreach (var propagatorValue in propagators!.Split(Constants.ConfigurationValues.Separator))
            {
                if (TryParsePropagator(propagatorValue, out var propagator))
                {
                    Propagators.Add(propagator.Value);
                }
                else if (configuration.FailFast)
                {
                    throw new NotSupportedException($"Propagator '{propagatorValue}' is not supported.");
                }
            }
        }
    }

    private static bool TryParsePropagator(string propagatorValue, [NotNullWhen(true)] out Propagator? propagator)
    {
        switch (propagatorValue)
        {
            case Constants.ConfigurationValues.Propagators.W3CTraceContext:
                propagator = Propagator.W3CTraceContext;
                break;
            case Constants.ConfigurationValues.Propagators.W3CBaggage:
                propagator = Propagator.W3CBaggage;
                break;
            case Constants.ConfigurationValues.Propagators.B3Multi:
                propagator = Propagator.B3Multi;
                break;
            case Constants.ConfigurationValues.Propagators.B3Single:
                propagator = Propagator.B3Single;
                break;
            default:
                propagator = null;
                Logger.Error($"Propagator '{propagatorValue}' is not supported. Skipping.");
                return false;
        }

        return true;
    }
}
