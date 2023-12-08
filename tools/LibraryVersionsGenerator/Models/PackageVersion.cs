// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal class PackageVersion
{
    public PackageVersion(string version)
    {
        Version = version;
    }

    public string Version { get; init; }
}
