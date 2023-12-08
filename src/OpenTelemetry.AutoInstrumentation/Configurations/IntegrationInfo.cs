// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal readonly struct IntegrationInfo
{
    public readonly string? Name;

    public readonly int Id;

    public IntegrationInfo(string integrationName)
    {
        Name = integrationName;
        Id = 0;
    }

    public IntegrationInfo(int integrationId)
    {
        Name = null;
        Id = integrationId;
    }
}
