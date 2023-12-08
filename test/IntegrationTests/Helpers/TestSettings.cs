// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

public class TestSettings
{
    public string? Arguments { get; set; } = null;

    public string PackageVersion { get; set; } = string.Empty;

    public string Framework { get; set; } = string.Empty;

    public TestAppStartupMode StartupMode { get; set; }
}
