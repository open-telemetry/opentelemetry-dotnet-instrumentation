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
    public static bool IsProcessWide(string key)
    {
        // These settings affect native, process-wide behavior.
        return key switch
        {
            ConfigurationKeys.FailFast => true,
            ConfigurationKeys.Traces.InstrumentationOptions.SqlClientNetFxILRewriteEnabled => true,
            _ => false
        };
    }

    public override string? GetString(string key)
    {
        return IsProcessWide(key) ? null : appSettings[key];
    }
}
#endif
