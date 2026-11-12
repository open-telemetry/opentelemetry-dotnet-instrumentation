// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Collections.Specialized;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal sealed class AppSettingsConfigurationSource(
    bool failFast,
    NameValueCollection appSettings)
    : StringConfigurationSource(failFast)
{
    public override string? GetString(string key)
    {
        // These settings affect native, process-wide behavior.
        if (key == ConfigurationKeys.FailFast
            || key == ConfigurationKeys.Traces.InstrumentationOptions.SqlClientNetFxILRewriteEnabled)
        {
            return null;
        }

        return appSettings[key];
    }
}
#endif
