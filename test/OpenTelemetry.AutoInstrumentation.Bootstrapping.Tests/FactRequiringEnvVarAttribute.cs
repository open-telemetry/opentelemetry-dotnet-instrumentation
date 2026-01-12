// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests;

internal sealed class FactRequiringEnvVarAttribute : FactAttribute
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
