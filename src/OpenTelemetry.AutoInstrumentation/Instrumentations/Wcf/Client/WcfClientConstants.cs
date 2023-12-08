// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

internal static class WcfClientConstants
{
    public const string ChannelFactoryTypeName = "System.ServiceModel.ChannelFactory";
    public const string InitializeEndpointMethodName = "InitializeEndpoint";
    public const string IntegrationName = nameof(TracerInstrumentation.WcfClient);
    public const string EndpointAddressTypeName = "System.ServiceModel.EndpointAddress";
    public const string ServiceEndpointTypeName = "System.ServiceModel.Description.ServiceEndpoint";
    public const string BindingTypeName = "System.ServiceModel.Channels.Binding";
    public const string ConfigurationTypeName = "System.Configuration.Configuration";
}
