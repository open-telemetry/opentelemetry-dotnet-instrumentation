// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Logging;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge.Integrations;

/// <summary>
/// NLog Target Collection integration.
/// This integration hooks into NLog's target collection methods to automatically
/// inject the OpenTelemetry target when the NLog bridge is enabled.
///
/// The integration targets NLog's LoggingConfiguration.GetConfiguredNamedTargets method
/// which is called when NLog retrieves the list of configured targets.
/// </summary>
[InstrumentMethod(
assemblyName: "NLog",
typeName: "NLog.Config.LoggingConfiguration",
methodName: "GetConfiguredNamedTargets",
returnTypeName: "NLog.Targets.Target[]",
parameterTypeNames: new string[0],
minimumVersion: "4.0.0",
maximumVersion: "6.*.*",
integrationName: "NLog",
type: InstrumentationType.Log)]
public static class TargetCollectionIntegration
{
#if NET
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static int _warningLogged;
#endif

    /// <summary>
    /// Intercepts the completion of NLog's GetConfiguredNamedTargets method.
    /// This method is called after NLog has retrieved its configured targets,
    /// allowing us to inject the OpenTelemetry target into the collection.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target being returned.</typeparam>
    /// <typeparam name="TReturn">The type of the return value (target array).</typeparam>
    /// <param name="instance">The LoggingConfiguration instance.</param>
    /// <param name="returnValue">The array of configured targets returned by NLog.</param>
    /// <param name="exception">Any exception that occurred during the method execution.</param>
    /// <param name="state">The call target state.</param>
    /// <returns>A CallTargetReturn containing the modified target array with the OpenTelemetry target injected.</returns>
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
#if NET
        // Check if ILogger bridge has been initialized and warn if so
        // This prevents conflicts between different logging bridges
        if (LoggerInitializer.IsInitializedAtLeastOnce)
        {
            if (Interlocked.Exchange(ref _warningLogged, 1) != default)
            {
                return new CallTargetReturn<TReturn>(returnValue);
            }

            Logger.Warning("Disabling addition of NLog bridge due to ILogger bridge initialization.");
            return new CallTargetReturn<TReturn>(returnValue);
        }
#endif

        // Only inject the target if the NLog bridge is enabled and we have a valid target array
        if (Instrumentation.LogSettings.Value.EnableNLogBridge && returnValue is Array targetsArray)
        {
            // Use the target initializer to inject the OpenTelemetry target
            var modifiedTargets = OpenTelemetryTargetInitializer<TReturn>.Initialize(targetsArray);
            return new CallTargetReturn<TReturn>(modifiedTargets);
        }

        // Return the original targets if injection is not enabled or not applicable
        return new CallTargetReturn<TReturn>(returnValue);
    }
}
