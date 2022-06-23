// <copyright file="TcpPortProvider.cs" company="OpenTelemetry Authors">
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
        TcpListener tcpListener = null;

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
