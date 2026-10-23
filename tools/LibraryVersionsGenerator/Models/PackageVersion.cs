// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal class PackageVersion
{
    public PackageVersion(
        string version,
        string[]? supportedTargetFrameworks = null,
        string[]? supportedExecutionFrameworks = null,
        string[]? supportedPlatforms = null,
        IReadOnlyDictionary<string, string>? buildProperties = null)
    {
        Version = version;
        SupportedTargetFrameworks = supportedTargetFrameworks ?? Array.Empty<string>();
        SupportedExecutionFrameworks = supportedExecutionFrameworks ?? Array.Empty<string>();
        SupportedPlatforms = supportedPlatforms ?? Array.Empty<string>();
        BuildProperties = buildProperties ?? new Dictionary<string, string>();
    }

    public string Version { get; init; }

    public string[] SupportedTargetFrameworks { get; init; }

    public string[] SupportedExecutionFrameworks { get; init; }

    public string[] SupportedPlatforms { get; init; }

    public IReadOnlyDictionary<string, string> BuildProperties { get; init; }
}
