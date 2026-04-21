// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

internal sealed class NameValueConfigurationSource : StringConfigurationSource
{
    private readonly NameValueCollection _nameValueCollection;

    public NameValueConfigurationSource(bool failFast, NameValueCollection nameValueCollection)
        : base(failFast)
    {
        _nameValueCollection = nameValueCollection;
    }

    public override string? GetString(string key)
    {
        return _nameValueCollection[key];
    }
}
