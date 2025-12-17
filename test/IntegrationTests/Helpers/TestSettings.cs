// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal sealed class TestSettings
{
    public string? Arguments { get; set; }

    public string PackageVersion { get; set; } = string.Empty;

    public string Framework { get; set; } = string.Empty;

    public TestAppStartupMode StartupMode { get; set; }
}
