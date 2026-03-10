// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy;

internal static class IntegrationConstants
{
    public const string RabbitMqByteCodeIntegrationName = "RabbitMq";
    public const string RabbitMqAssemblyName = "RabbitMQ.Client";
    public const string ModelBaseTypeName = "RabbitMQ.Client.Impl.ModelBase";
    public const string DefaultBasicConsumerTypeName = "RabbitMQ.Client.DefaultBasicConsumer";
    public const string AsyncDefaultBasicConsumerTypeName = "RabbitMQ.Client.AsyncDefaultBasicConsumer";
    public const string ModelGeneratedTypeName = "RabbitMQ.Client.Framing.Impl.Model";
    public const string BasicGetResultTypeName = "RabbitMQ.Client.BasicGetResult";
    public const string BasicPropertiesInterfaceTypeName = "RabbitMQ.Client.IBasicProperties";
    public const string BasicGetMethodName = "BasicGet";
    public const string HandleBasicDeliverMethodName = "HandleBasicDeliver";
    public const string BasicPublishMethodName = "_Private_BasicPublish";
    public const string Min5SupportedVersion = "5.0.0";
    public const string Max5SupportedVersion = "5.*.*";
    public const string Min6SupportedVersion = "6.0.0";
    public const string Max6SupportedVersion = "6.*.*";
}
