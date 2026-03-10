// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class FailFastSettings : Settings
{
    public bool FailFast { get; private set; }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        FailFast = configuration.GetBool(ConfigurationKeys.FailFast) ?? false;
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        FailFast = configuration.FailFast;
    }
}
