// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Sockets;

namespace TestApplication.Http.NetFramework.Helpers;

internal static class TcpPortProvider
{
    public static int GetOpenPort()
    {
        TcpListener? tcpListener = null;

        try
        {
            tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();

            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;

            return port;
        }
        finally
        {
            tcpListener?.Stop();
        }
    }
}
