// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
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
    public IList<Propagator> Propagators { get; private set; } = [];

    /// <summary>
    /// Gets the attribute limits.
    /// </summary>
    public AttributeLimits AttributeLimits { get; private set; } = new();

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        AttributeLimits = new AttributeLimits(
            attributeCountLimit: configuration.GetInt32(ConfigurationKeys.Sdk.AttributeCountLimit),
            attributeValueLengthLimit: configuration.GetInt32(ConfigurationKeys.Sdk.AttributeValueLengthLimit));

        Propagators = ParsePropagator(configuration);
    }

    protected override void OnLoadFile(Conf configuration)
    {
        AttributeLimits = configuration.AttributeLimits;

        if (configuration.Propagator == null)
        {
            return;
        }

        var seenPropagators = new HashSet<string>();
        var resolvedPropagators = new List<string>();

        if (configuration.Propagator.Composite is Dictionary<string, object> compositeDict)
        {
            foreach (var key in compositeDict.Keys)
            {
                if (seenPropagators.Add(key))
                {
                    resolvedPropagators.Add(key);
                }
            }
        }

        if (!string.IsNullOrEmpty(configuration.Propagator.CompositeList))
        {
            var list = configuration.Propagator.CompositeList!.Split(Constants.ConfigurationValues.Separator);
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
                    Propagators.Add(Propagator.W3CTraceContext);
                    break;
                case Constants.ConfigurationValues.Propagators.W3CBaggage:
                    Propagators.Add(Propagator.W3CBaggage);
                    break;
                case Constants.ConfigurationValues.Propagators.B3Multi:
                    Propagators.Add(Propagator.B3Multi);
                    break;
                case Constants.ConfigurationValues.Propagators.B3Single:
                    Propagators.Add(Propagator.B3Single);
                    break;
                default:
                    var unsupportedMessage = $"Propagator '{propagatorName}' is not supported.";
                    Logger.Error(unsupportedMessage);

                    if (configuration.FailFast == true)
                    {
                        throw new NotSupportedException(unsupportedMessage);
                    }

                    break;
            }
        }
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
