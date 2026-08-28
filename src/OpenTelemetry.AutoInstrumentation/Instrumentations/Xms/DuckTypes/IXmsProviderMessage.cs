// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// wraps IBM.XMS.Provider.ProviderMessage (the async-delivery message shape passed to
// IBM.XMS.Client.Impl.XmsProviderMessageListener.OnMessage - distinct from IBM.XMS.IMessage)
internal interface IXmsProviderMessage : IXmsPropertyContext
{
    public string? JMSMessageID { get; }

    public string? JMSCorrelationID { get; }

    public string? JMSDestinationAsString { get; }

    public void SetStringProperty(string name, string value);
}
