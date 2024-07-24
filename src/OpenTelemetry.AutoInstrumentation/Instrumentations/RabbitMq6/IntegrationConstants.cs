// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6;

internal static class IntegrationConstants
{
    public const string RabbitMqByteCodeIntegrationName = "RabbitMq6";
    public const string RabbitMqAssemblyName = "RabbitMQ.Client";
    public const string ModelBaseTypeName = "RabbitMQ.Client.Impl.ModelBase";
    public const string EventingBasicConsumerTypeName = "RabbitMQ.Client.Events.EventingBasicConsumer";
    public const string AsyncEventingBasicConsumerTypeName = "RabbitMQ.Client.Events.AsyncEventingBasicConsumer";
    public const string ModelGeneratedTypeName = "RabbitMQ.Client.Framing.Impl.Model";
    public const string BasicGetResultTypeName = "RabbitMQ.Client.BasicGetResult";
    public const string BasicPropertiesInterfaceTypeName = "RabbitMQ.Client.IBasicProperties";
    public const string BasicGetMethodName = "BasicGet";
    public const string HandleBasicDeliverMethodName = "HandleBasicDeliver";
    public const string BasicPublishMethodName = "_Private_BasicPublish";
    public const string MinSupportedVersion = "6.0.0";
    public const string MaxSupportedVersion = "6.*.*";
}
