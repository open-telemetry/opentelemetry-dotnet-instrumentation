// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// EnvironmentSetter is initializing the OTEL_* environemtal variables
/// with provided values if they are not already set.
/// </summary>
internal class EnvironmentInitializer
{
    public static void Initialize(NameValueCollection nameValueCollection)
    {
        foreach (var setting in nameValueCollection.AllKeys)
        {
            if (setting == null)
            {
                continue;
            }

            if (setting != ConfigurationKeys.FileBasedConfiguration.Enabled
                && setting != ConfigurationKeys.FileBasedConfiguration.FileName)
            {
                // YAML selection is process-level. Managed settings are read from
                // the current AppDomain's appSettings instead of being promoted.
                continue;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(setting)))
            {
                // already set via env var - to not override
                continue;
            }

            Environment.SetEnvironmentVariable(setting, nameValueCollection[setting]);
        }
    }
}
