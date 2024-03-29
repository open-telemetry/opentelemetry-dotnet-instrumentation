// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Sockets;

namespace IntegrationTests.Helpers;

/// <summary>
/// Helper class that tries to provide unique ports numbers across processes and threads in the same machine.
/// Used avoid port conflicts in concurrent tests that use the Agent, IIS, HttpListener, HttpClient, etc.
/// This class cannot guarantee a port is actually available, but should help avoid most conflicts.
/// </summary>
public static class TcpPortProvider
{
    public static int GetOpenPort()
    {
        TcpListener? tcpListener = null;

        try
        {
            tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();

            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;

            return port;
        }
        finally
        {
            tcpListener?.Stop();
        }
    }
}
