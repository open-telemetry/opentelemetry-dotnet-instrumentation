// <copyright file="JaegerThriftClientTransport.cs" company="OpenTelemetry Authors">
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

using System;
using System.IO;
using System.Net.Sockets;
using Thrift.Transport;

namespace Datadog.Trace.Agent.Jaeger
{
    internal class JaegerThriftClientTransport : TTransport
    {
        private readonly IJaegerClient client;
        private readonly MemoryStream byteStream;
        private bool isDisposed;

        public JaegerThriftClientTransport(string host, int port, MemoryStream stream = null, IJaegerClient client = null)
        {
            this.byteStream = stream ?? new MemoryStream();
            this.client = client ?? new JaegerUdpClient();
            this.client.Connect(host, port);
        }

        public override bool IsOpen => this.client.Connected;

        public override void Close()
        {
            this.client.Close();
        }

        public override int Flush()
        {
            ArraySegment<byte> buffer;

#if !NET45
            // GetBuffer returns the underlying storage, which saves an allocation over ToArray.
            if (!this.byteStream.TryGetBuffer(out buffer))
            {
#endif
            buffer = new ArraySegment<byte>(this.byteStream.ToArray(), 0, (int)this.byteStream.Length);
#if !NET45
            }
#endif

            if (buffer.Count == 0)
            {
                return 0;
            }

            try
            {
                return this.client.Send(buffer.Array, buffer.Offset, buffer.Count);
            }
            catch (SocketException se)
            {
                throw new TTransportException(TTransportException.ExceptionType.Unknown, $"Cannot flush because of socket exception. UDP Packet size was {buffer.Count}. Exception message: {se.Message}");
            }
            catch (Exception e)
            {
                throw new TTransportException(TTransportException.ExceptionType.Unknown, $"Cannot flush closed transport. {e.Message}");
            }
            finally
            {
                this.byteStream.SetLength(0);
            }
        }

        public override void Write(byte[] buffer, int offset, int length)
        {
            this.byteStream.Write(buffer, offset, length);
        }

        public override string ToString()
        {
            return $"{nameof(JaegerThriftClientTransport)}(Client={this.client.RemoteEndPoint})";
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.isDisposed && disposing)
            {
                this.byteStream?.Dispose();
                this.client?.Dispose();
            }

            this.isDisposed = true;
        }
    }
}
