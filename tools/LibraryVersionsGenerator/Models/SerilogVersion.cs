// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal sealed class SerilogVersion : PackageVersion
{
    public SerilogVersion(string version)
        : base(version)
    {
    }

    [PackageDependency("Serilog.Extensions.Hosting", "SerilogExtensionsHosting")]
    public required string SerilogExtensionsHostingVersion { get; set; }

    [PackageDependency("Serilog.Sinks.OpenTelemetry", "SerilogSinksOpenTelemetry")]
    public required string SerilogSinksOpenTelemetryVersion { get; set; }
}
