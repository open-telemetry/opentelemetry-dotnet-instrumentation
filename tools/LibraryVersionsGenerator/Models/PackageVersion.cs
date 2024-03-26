// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal class PackageVersion
{
    public PackageVersion(string version, string[]? supportedFrameworks = null, string[]? supportedPlatforms = null)
    {
        Version = version;
        SupportedFrameworks = supportedFrameworks ?? Array.Empty<string>();
        SupportedPlatforms = supportedPlatforms ?? Array.Empty<string>();
    }

    public string Version { get; init; }

    public string[] SupportedFrameworks { get; init; }

    public string[] SupportedPlatforms { get; init; }
}
