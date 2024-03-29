// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace OpenTelemetry.AutoInstrumentation.BuildTasks;

/// <summary>
/// Implements a build task that checks if the project references any packages that can be instrumented and if
/// the respective adapter packages, necessary to instrument the target, are or not part of the project.
/// </summary>
public class CheckForInstrumentationPackages : Microsoft.Build.Utilities.Task
{
    private const string LogPrefix = "OpenTelemetry.AutoInstrumentation: ";

    private MessageImportance _logMessageImportance = MessageImportance.Low;

    /// <summary>
    /// Gets or sets the list of instrumentation target items.
    /// </summary>
    /// <remarks>
    /// See the <code>InstrumentationTarget</code>"/> items on the <code>.targets</code> file for a list of the
    /// required metadata.
    /// </remarks>
    [Required]
    public ITaskItem[]? InstrumentationTargetItems { get; set; }

    /// <summary>
    /// Gets or sets the list of runtime copy local items.
    /// </summary>
    /// <remarks>
    /// This list generated by the standard build target <code>ResolvePackageAssets</code>.
    /// </remarks>
    [Required]
    public ITaskItem[]? RuntimeCopyLocalItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use verbose logging.
    /// </summary>
    public bool UseVerboseLog { get; set; }

    /// <summary>
    /// Checks if the project references any packages that can be instrumented and if the respective adapter packages
    /// were already added to the project.
    /// </summary>
    /// <returns>True, if the task completed without errors, false otherwise.</returns>
    public override bool Execute()
    {
        _logMessageImportance = UseVerboseLog ? MessageImportance.High : MessageImportance.Low;
        try
        {
            return ExecuteImplementation();
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            throw;
        }
    }

    private bool ExecuteImplementation()
    {
        if (RuntimeCopyLocalItems is null || RuntimeCopyLocalItems.Length == 0)
        {
            Log.LogMessage(
                _logMessageImportance,
                $"{LogPrefix}empty {nameof(RuntimeCopyLocalItems)}, skipping check for instrumentation packages.");
            return true;
        }

        // Put runtime local items in a dictionary for easy lookup.
        var runtimeCopyLocalItemsDictionary = new Dictionary<string, ITaskItem>(
            RuntimeCopyLocalItems.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var item in RuntimeCopyLocalItems)
        {
            var nuGetPackageId = item.GetMetadata("NuGetPackageId");
            if (!string.IsNullOrWhiteSpace(nuGetPackageId))
            {
                runtimeCopyLocalItemsDictionary[nuGetPackageId] = item;
            }
        }

        // Process each instrumentation target item.
        foreach (var instrumentationTarget in InstrumentationTargetItems ?? Array.Empty<ITaskItem>())
        {
            MissingInstrumentationAdapterPackage(instrumentationTarget, runtimeCopyLocalItemsDictionary);
        }

        // If missing any instrumentation adapter package then fail the build by returning false, the method
        // Log.LogError was already called.
        return !Log.HasLoggedErrors;
    }

    private void MissingInstrumentationAdapterPackage(
        ITaskItem instrumentationTargetTaskItem,
        Dictionary<string, ITaskItem> runtimeCopyLocalItemsDictionary)
    {
        var item = new InstrumentationTarget(instrumentationTargetTaskItem);
        if (!runtimeCopyLocalItemsDictionary.TryGetValue(item.TargetNuGetPackageId, out var runtimeCopyLocalItem))
        {
            var msg =
                $"{LogPrefix}project doesn't reference '{item.TargetNuGetPackageId}', " +
                $"no need for the instrumentation package {item.FriendlyInstrumentationPackage}.";
            Log.LogMessage(_logMessageImportance, msg);
            return;
        }

        // Check if the local item is in the targeted version range.
        var runtimeNuGetPackageVersion = NuGetVersion.Parse(runtimeCopyLocalItem.GetMetadata("NuGetPackageVersion"));
        if (!item.TargetNuGetPackageVersionRange.Satisfies(runtimeNuGetPackageVersion))
        {
            var msg =
                $"{LogPrefix}project references '{item.TargetNuGetPackageId}' " +
                $"but not in the range {item.TargetNuGetPackageVersionRange}, " +
                $"no need for the instrumentation package {item.FriendlyInstrumentationPackage}.";
            Log.LogMessage(_logMessageImportance, msg);
            return;
        }

        // The application is using a version of the target package that is in the proper range.
        // Now check for the instrumentation package.
        if (runtimeCopyLocalItemsDictionary.TryGetValue(
            item.InstrumentationNuGetPackageId,
            out ITaskItem? runtimeInstrumentationNuGetPackage))
        {
            // Check if the instrumentation package version is in the expected one.
            var runtimeInstrumentationNuGetPackageVersion = NuGetVersion.Parse(
                runtimeInstrumentationNuGetPackage.GetMetadata("NuGetPackageVersion"));
            if (item.InstrumentationNuGetPackageVersion.Equals(runtimeInstrumentationNuGetPackageVersion))
            {
                var msg =
                    $"{LogPrefix}project already references {item.FriendlyInstrumentationPackage} " +
                    $"required to instrument '{item.TargetNuGetPackageId}' version {item.TargetNuGetPackageVersionRange}.";
                Log.LogMessage(_logMessageImportance, msg);
                return;
            }
        }

        var errorMsg = $"{LogPrefix}add a reference to the instrumentation package {item.FriendlyInstrumentationPackage} " +
            $"or add '{item.TargetNuGetPackageId}' to the property 'SkippedInstrumentations' to suppress this error.";
        Log.LogError(errorMsg);
    }
}
