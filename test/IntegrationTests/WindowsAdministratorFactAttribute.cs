// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

namespace IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class WindowsAdministratorFactAttribute : FactAttribute
{
    public WindowsAdministratorFactAttribute()
        : base()
    {
        Skip = EnvironmentTools.IsWindowsAdministrator() ? null : "This test requires Windows Administrator privileges.";
    }
}
