// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Builds the <see cref="IConfiguration"/> handed to the OpenTelemetry SDK so that settings the SDK
/// reads on its own (sampler, batch processors, metric readers, ...) honor the current AppDomain's
/// appSettings without promoting them to process environment variables.
/// </summary>
internal static class SdkAppSettingsConfiguration
{
    private const string EnvironmentVariablePrefix = "OTEL_";

    public static IConfiguration Create(NameValueCollection appSettings)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var key in appSettings.AllKeys)
        {
            if (key == null
                || !key.StartsWith(EnvironmentVariablePrefix, StringComparison.Ordinal)
                || AppSettingsConfigurationSource.IsProcessWide(key))
            {
                continue;
            }

            // Environment variables take precedence over appSettings. An empty value is treated as unset.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            values[key] = appSettings[key];
        }

        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(values)
            .Build();
    }
}
#endif
