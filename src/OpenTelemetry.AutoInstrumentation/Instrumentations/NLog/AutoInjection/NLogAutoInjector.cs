// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.AutoInjection;

internal static class NLogAutoInjector
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static int _attempted;

    public static void EnsureConfigured()
    {
        if (Interlocked.Exchange(ref _attempted, 1) != 0)
        {
            return;
        }

        try
        {
            var nlogLogManager = Type.GetType("NLog.LogManager, NLog");
            if (nlogLogManager is null)
            {
                return;
            }

            var configurationProperty = nlogLogManager.GetProperty("Configuration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (configurationProperty is null)
            {
                return;
            }

            var configuration = configurationProperty.GetValue(null);
            if (configuration is null)
            {
                var configurationType = Type.GetType("NLog.Config.LoggingConfiguration, NLog");
                configuration = Activator.CreateInstance(configurationType!);
                configurationProperty.SetValue(null, configuration);
            }

            // Create the OpenTelemetry target instance
            var otelTargetType = Type.GetType("OpenTelemetry.AutoInstrumentation.NLogTarget.OpenTelemetryTarget, OpenTelemetry.AutoInstrumentation.NLogTarget");
            if (otelTargetType is null)
            {
                Logger.Warning("NLog auto-injection skipped: target type not found.");
                return;
            }

            var otelTarget = Activator.CreateInstance(otelTargetType);

            // Add target to configuration
            var addTargetMethod = configuration!.GetType().GetMethod("AddTarget", BindingFlags.Instance | BindingFlags.Public);
            addTargetMethod?.Invoke(configuration, new object?[] { "otlp", otelTarget });

            // Create rule: * -> otlp (minlevel: Trace)
            var loggingRuleType = Type.GetType("NLog.Config.LoggingRule, NLog");
            var targetType = Type.GetType("NLog.Targets.Target, NLog");
            var logLevelType = Type.GetType("NLog.LogLevel, NLog");
            var traceLevel = logLevelType?.GetProperty("Trace", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            var rule = Activator.CreateInstance(loggingRuleType!, new object?[] { "*", traceLevel, otelTarget });

            var loggingRulesProp = configuration.GetType().GetProperty("LoggingRules", BindingFlags.Instance | BindingFlags.Public);
            var rulesList = loggingRulesProp?.GetValue(configuration) as System.Collections.IList;
            rulesList?.Add(rule);

            // Apply configuration
            var reconfigMethod = nlogLogManager.GetMethod("ReconfigExistingLoggers", BindingFlags.Static | BindingFlags.Public);
            reconfigMethod?.Invoke(null, null);

            Logger.Information("NLog OpenTelemetryTarget auto-injected.");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "NLog OpenTelemetryTarget auto-injection failed.");
        }
    }
}
