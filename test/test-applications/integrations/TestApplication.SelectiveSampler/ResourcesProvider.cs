// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

// TODO: verify if needed
namespace TestApplication.SelectiveSampler;

internal class ResourcesProvider
{
    public static Resource Resource { get; private set; } = Resource.Empty;

    public static void Configure(ResourceBuilder builder)
    {
        Resource = builder.Build();
    }
}
