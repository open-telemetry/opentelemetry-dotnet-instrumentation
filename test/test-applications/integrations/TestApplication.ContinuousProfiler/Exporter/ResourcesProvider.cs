// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace TestApplication.ContinuousProfiler;

internal static class ResourcesProvider
{
    public static Resource Resource { get; private set; } = Resource.Empty;

    public static void Configure(ResourceBuilder builder)
    {
        Resource = builder.Build();
    }
}
