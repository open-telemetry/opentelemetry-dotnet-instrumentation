// <copyright file="WcfClientConstants.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
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
