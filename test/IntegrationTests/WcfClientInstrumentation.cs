// <copyright file="WcfClientInstrumentation.cs" company="OpenTelemetry Authors">
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
#if NETFRAMEWORK

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

internal static class WcfClientInstrumentation
{
    public const string NetTcpBindingMessageVersion = "Soap12 (http://www.w3.org/2003/05/soap-envelope) Addressing10 (http://www.w3.org/2005/08/addressing)";
    public const string HttpBindingMessageVersion = "Soap11 (http://schemas.xmlsoap.org/soap/envelope/) AddressingNone (http://schemas.microsoft.com/ws/2005/05/addressing/none)";
    public const string NetTcpChannelScheme = "net.tcp";
    public const string HttpChannelScheme = "http";

    public static bool ValidateBasicSpanExpectations(
        Span span,
        string expectedChannelScheme,
        string expectedChannelPath,
        string expectedPeerName,
        int expectedPeerPort,
        string expectedMessageVersion)
    {
        var attributes = span.Attributes;
        var rpcSystem = ExtractAttribute(attributes, "rpc.system");
        var rpcService = ExtractAttribute(attributes, "rpc.service");
        var rpcMethod = ExtractAttribute(attributes, "rpc.method");
        var soapMessageVersion = ExtractAttribute(attributes, "soap.message_version");
        var netPeerPort = ExtractAttribute(attributes, "net.peer.port");
        var netPeerName = ExtractAttribute(attributes, "net.peer.name");
        var channelSchemeTag = ExtractAttribute(attributes, "wcf.channel.scheme");
        var channelPath = ExtractAttribute(attributes, "wcf.channel.path");
        return span.Kind == Span.Types.SpanKind.Client &&
               rpcSystem.Value.StringValue == "dotnet_wcf" &&
               rpcService.Value.StringValue == "http://opentelemetry.io/StatusService" &&
               rpcMethod.Value.StringValue == "Ping" &&
               netPeerName.Value.StringValue == expectedPeerName &&
               netPeerPort.Value.IntValue == expectedPeerPort &&
               channelSchemeTag.Value.StringValue == expectedChannelScheme &&
               soapMessageVersion.Value.StringValue == expectedMessageVersion &&
               channelPath.Value.StringValue == expectedChannelPath;
    }

    public static bool ValidateSpanSuccessStatus(Span span)
    {
        return span.Status == null;
    }

    private static KeyValue ExtractAttribute(IEnumerable<KeyValue> attributes, string key)
    {
        return attributes.Single(kv => kv.Key == key);
    }
}
#endif
