// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal sealed class NLogVersion : PackageVersion
{
    public NLogVersion(string version)
        : base(version)
    {
    }

    [PackageDependency("NLog.Extensions.Logging", "NLogExtensionsLogging")]
    public required string NLogExtensionsLoggingVersion { get; set; }
}
