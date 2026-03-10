// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.SelectiveSampler.Plugins;

internal static class SelectiveSamplerPluginHelper
{
    public static Tuple<uint, TimeSpan, TimeSpan, object> GetTestConfiguration()
    {
        return Tuple.Create<uint, TimeSpan, TimeSpan, object>(50u, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5000), JsonConsoleExporter.Instance);
    }
}
