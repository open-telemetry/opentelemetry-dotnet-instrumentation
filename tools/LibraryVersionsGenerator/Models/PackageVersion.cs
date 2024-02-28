// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal class PackageVersion
{
    public PackageVersion(string version, params string[] supportedFrameworks)
    {
        Version = version;
        SupportedFrameworks = supportedFrameworks;
    }

    public string Version { get; init; }

    public string[] SupportedFrameworks { get; set; }
}
