// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class FailFastSettings : Settings
{
    public bool FailFast { get; private set; }

    protected override void OnLoad(Configuration configuration)
    {
        FailFast = configuration.GetBool(ConfigurationKeys.FailFast) ?? false;
    }
}
