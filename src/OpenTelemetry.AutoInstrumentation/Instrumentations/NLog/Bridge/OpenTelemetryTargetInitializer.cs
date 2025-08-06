// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;

/// <summary>
/// Initializer for injecting the OpenTelemetry target into NLog's targets collection.
/// This class handles the dynamic injection of the OpenTelemetry target into existing
/// NLog target arrays when the instrumentation is enabled.
/// </summary>
/// <typeparam name="TTarget">The type of the target array being modified.</typeparam>
internal static class OpenTelemetryTargetInitializer<TTarget>
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Initializes the OpenTelemetry target and injects it into the provided target array.
    /// This method creates a new array that includes all existing targets plus the
    /// OpenTelemetry target for capturing log events.
    /// </summary>
    /// <param name="originalTargets">The original array of NLog targets.</param>
    /// <returns>A new array containing the original targets plus the OpenTelemetry target.</returns>
    public static TTarget Initialize(Array originalTargets)
    {
        try
        {
            // Get the OpenTelemetry target instance
            var openTelemetryTarget = OpenTelemetryNLogTarget.Instance;

            // Create a new array with space for one additional target
            var newLength = originalTargets.Length + 1;
            var elementType = originalTargets.GetType().GetElementType()!;
            var newTargets = Array.CreateInstance(elementType, newLength);

            // Copy existing targets to the new array
            Array.Copy(originalTargets, newTargets, originalTargets.Length);

            // Add the OpenTelemetry target at the end
            newTargets.SetValue(openTelemetryTarget, originalTargets.Length);

            Logger.Debug("Successfully injected OpenTelemetry NLog target into targets collection.");

            // Cast the new array to the expected return type
            return (TTarget)(object)newTargets;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to inject OpenTelemetry NLog target into targets collection.");
            // Return the original array if injection fails
            return (TTarget)(object)originalTargets;
        }
    }
}
