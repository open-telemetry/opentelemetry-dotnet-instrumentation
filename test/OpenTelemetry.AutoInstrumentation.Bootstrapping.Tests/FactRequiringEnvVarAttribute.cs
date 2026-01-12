// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests;

#pragma warning disable CA1812 // Mark members as static. There is some issue in dotnet format.
internal sealed class FactRequiringEnvVarAttribute : FactAttribute
#pragma warning restore CA1812 // Mark members as static. There is some issue in dotnet format.
{
    private const string EnvVar = "BOOSTRAPPING_TESTS";

    public FactRequiringEnvVarAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvVar)))
        {
            Skip = $"Ignore as {EnvVar} is not set";
        }
    }
}
