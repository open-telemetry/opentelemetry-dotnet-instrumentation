// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

namespace TestApplication.SelectiveSampler.Plugins;

internal static class SelectiveSamplerPluginHelper
{
    public static SelectiveSamplerConfiguration GetTestConfiguration()
    {
        return new SelectiveSamplerConfiguration()
        {
            SamplingInterval = 50u,
            ExportInterval = TimeSpan.FromMilliseconds(50),
            ExportTimeout = TimeSpan.FromMilliseconds(5000),
            Exporter = JsonConsoleExporter.Instance
        };
    }
}
