// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
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
    public IReadOnlyList<Propagator> Propagators { get; private set; } = [];

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        Propagators = ParsePropagator(configuration);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        Propagators = configuration.Propagator?.GetEnabledPropagators() ?? [];
    }

    private static List<Propagator> ParsePropagator(Configuration configuration)
    {
        var propagatorEnvVar = configuration.GetString(ConfigurationKeys.Sdk.Propagators);
        var propagators = new List<Propagator>();
        var seenPropagators = new HashSet<string>();

        if (string.IsNullOrEmpty(propagatorEnvVar))
        {
            return propagators;
        }

        var propagatorNames = propagatorEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var propagatorName in propagatorNames)
        {
            if (seenPropagators.Contains(propagatorName))
            {
                var message = $"Duplicate propagator '{propagatorName}' found.";
                if (configuration.FailFast)
                {
                    Logger.Error(message);
                    throw new NotSupportedException(message);
                }

                Logger.Warning(message);
                continue;
            }

            seenPropagators.Add(propagatorName);

            switch (propagatorName)
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
                default:
                    var unsupportedMessage = $"Propagator '{propagatorName}' is not supported.";
                    Logger.Error(unsupportedMessage);

                    if (configuration.FailFast)
                    {
                        throw new NotSupportedException(unsupportedMessage);
                    }

                    break;
            }
        }

        return propagators;
    }
}
