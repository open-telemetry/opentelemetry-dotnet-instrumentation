// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class ResourceHelper
{
    public static Resource AggregateResources(params BaseProvider?[] providers)
    {
        var resource = Resource.Empty;

        foreach (var provider in providers)
        {
            if (provider == null)
            {
                continue;
            }

            try
            {
                var providerResource = provider.GetResource();
                if (providerResource != null)
                {
                    resource = resource.Merge(providerResource);
                }
            }
            catch (Exception)
            {
                // intentionally empty
            }
        }

        return resource;
    }
}
