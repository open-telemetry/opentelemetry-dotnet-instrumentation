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
    public override string? GetString(string key) => appSettings[key];
}
#endif
