// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

internal static class NetworkAttributes
{
    internal static class Keys
    {
        public const string ServerAddress = "server.address";
        public const string ServerPort = "server.port";
        public const string NetworkType = "network.type";
        public const string NetworkPeerAddress = "network.peer.address";
        public const string NetworkPeerPort = "network.peer.port";
    }

    internal static class Values
    {
        public const string IpV4NetworkType = "ipv4";
        public const string IpV6NetworkType = "ipv6";
    }
}
