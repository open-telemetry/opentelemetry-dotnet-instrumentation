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
internal static class TcpPortProvider
{
    private const int MaxAttempts = 10;

    // Ports already handed out during this process. Because GetOpenPort releases the port
    // it discovers, the OS can immediately hand back the same ephemeral port on the next
    // call; remembering what we returned prevents two consecutive calls (e.g. the HTTP and
    // net.tcp ports used by the WCF test servers) from colliding on the same number.
    private static readonly HashSet<int> HandedOutPorts = new();
    private static readonly object SyncRoot = new();

    public static int GetOpenPort()
    {
        lock (SyncRoot)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var port = FindFreePort();
                if (HandedOutPorts.Add(port))
                {
                    return port;
                }
            }

            // Give up on uniqueness after several attempts and return the last candidate.
            var fallback = FindFreePort();
            HandedOutPorts.Add(fallback);
            return fallback;
        }
    }

    private static int FindFreePort()
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
#if NET
            tcpListener?.Dispose();
#else
            tcpListener?.Stop();
#endif

        }
    }
}
