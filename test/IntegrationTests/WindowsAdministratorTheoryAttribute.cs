// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class WindowsAdministratorTheoryAttribute : TheoryAttribute
{
    public WindowsAdministratorTheoryAttribute()
        : base()
    {
        Skip = WindowsAdministratorFactAttribute.GetSkipReason();
    }
}
