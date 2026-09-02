// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// wraps IBM.XMS.IMessageProducer
internal interface IXmsMessageProducer
{
    public object? Destination { get; }
}
