// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// wraps IBM.XMS.IMessage
internal interface IXmsMessage : IXmsPropertyContext
{
    public string? JMSMessageID { get; }

    public string? JMSCorrelationID { get; }

    public object? JMSDestination { get; }

    public void SetStringProperty(string name, string value);
}
