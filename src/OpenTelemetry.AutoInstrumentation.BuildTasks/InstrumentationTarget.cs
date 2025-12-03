// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace OpenTelemetry.AutoInstrumentation.BuildTasks;

/// <summary>
/// Helper type that represents an instrumentation target as defined in a project file.
/// The target is defined as an item of type 'InstrumentationTarget' in a  project file.
/// </summary>
internal struct InstrumentationTarget
{
    private const string ItemName = "InstrumentationTarget";

    private readonly ITaskItem _taskItem;

    private VersionRange? _targetNuGetPackageVersionRange;
    private string? _instrumentationNuGetPackageId;
    private NuGetVersion? _instrumentationNuGetPackageVersion;

    public InstrumentationTarget(ITaskItem? instrumentationTargetTaskItem)
    {
        _taskItem = instrumentationTargetTaskItem ?? throw new ArgumentNullException(nameof(instrumentationTargetTaskItem));
    }

    private delegate bool TryParser<T>(string value, out T parsedValue);

    public string TargetNuGetPackageId
    {
        get
        {
            return string.IsNullOrWhiteSpace(_taskItem.ItemSpec)
                ? throw new ArgumentException($"An '{ItemName}' item must have a non-empty 'Include' attribute")
                : _taskItem.ItemSpec;
        }
    }

    public VersionRange TargetNuGetPackageVersionRange
    {
        get
        {
            _targetNuGetPackageVersionRange ??= ReadAndParseMetadata<VersionRange>(
                "TargetNuGetPackageVersionRange", VersionRange.TryParse);

            return _targetNuGetPackageVersionRange;
        }
    }

    public string InstrumentationNuGetPackageId
    {
        get
        {
            _instrumentationNuGetPackageId ??= ReadAndParseMetadata(
                "InstrumentationNuGetPackageId",
                (string value, out string parsedValue) =>
                {
                    parsedValue = value;
                    return true;
                });

            return _instrumentationNuGetPackageId;
        }
    }

    public NuGetVersion InstrumentationNuGetPackageVersion
    {
        get
        {
            _instrumentationNuGetPackageVersion ??= ReadAndParseMetadata<NuGetVersion>(
                "InstrumentationNuGetPackageVersion", NuGetVersion.TryParse);

            return _instrumentationNuGetPackageVersion;
        }
    }

    public string FriendlyInstrumentationPackage =>
        $"'{InstrumentationNuGetPackageId}' version {InstrumentationNuGetPackageVersion}";

    /// <summary>
    /// Helper method that reads and parses metadata items.
    /// </summary>
    private T ReadAndParseMetadata<T>(string metadataName, TryParser<T> tryParser)
    {
        var value = _taskItem.GetMetadata(metadataName);
        if (string.IsNullOrWhiteSpace(value) || !tryParser(value, out T parsedValue))
        {
            throw new ArgumentException(
                $"An '{ItemName}' item must have a valid '{metadataName}' metadata attribute");
        }

        return parsedValue;
    }
}
