// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;

internal static class IntegrationConstants
{
    public const string IntegrationName = "Xms";
    public const string XmsAssemblyName = "amqmxmsstd";
    public const string MessageProducerTypeName = "IBM.XMS.Client.Impl.XmsMessageProducerImpl";
    public const string MessageConsumerTypeName = "IBM.XMS.Client.Impl.XmsMessageConsumerImpl";
    public const string MessageListenerTypeName = "IBM.XMS.Client.Impl.XmsProviderMessageListener";
    public const string MessageInterfaceTypeName = "IBM.XMS.IMessage";
    public const string ProviderMessageTypeName = "IBM.XMS.Provider.ProviderMessage";
    public const string DestinationInterfaceTypeName = "IBM.XMS.IDestination";
    public const string DeliveryModeTypeName = "IBM.XMS.DeliveryMode";
    public const string SendMethodName = "Send";
    public const string ReceiveMethodName = "Receive";
    public const string ReceiveNoWaitMethodName = "ReceiveNoWait";
    public const string OnMessageMethodName = "OnMessage";
    public const string MinVersion = "9.0.0";
    public const string MaxVersion = "10.65535.65535";

    // IBM.XMS.XMSC.WMQ_RESOLVED_QUEUE_MANAGER_ID. Not referenced via IBM.XMS at compile time,
    // so the literal is duplicated here; value format is "<QMNAME>_YYYY-MM-DD_HH.MM.SS" and
    // arrives space-padded from a fixed-length MQ character field (callers must Trim()).
    public const string ResolvedQueueManagerIdPropertyName = "XMSC_WMQ_RESOLVED_QUEUE_MANAGER_ID";
}
