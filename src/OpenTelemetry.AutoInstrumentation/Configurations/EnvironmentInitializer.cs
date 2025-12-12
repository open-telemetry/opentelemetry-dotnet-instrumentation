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
    private const string VariablePrefix = "OTEL_";

    public static void Initialize(NameValueCollection nameValueCollection)
    {
        foreach (var setting in nameValueCollection.AllKeys)
        {
            if (setting == null)
            {
                continue;
            }

            if (!setting.StartsWith(VariablePrefix, StringComparison.Ordinal))
            {
                // not OTEL_ setting
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
