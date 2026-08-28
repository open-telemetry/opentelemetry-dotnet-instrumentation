// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// wraps IBM.XMS.IDestination
internal interface IXmsDestination
{
    public string? Name { get; }
}
