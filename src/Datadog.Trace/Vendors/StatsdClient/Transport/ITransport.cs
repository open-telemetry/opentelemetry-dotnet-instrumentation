//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
using System;

namespace Datadog.Trace.Vendors.StatsdClient.Transport
{
    internal enum TransportType
    {
        UDS,
        UDP,
        NamedPipe,
    }

    internal interface ITransport : IDisposable
    {
        TransportType TransportType { get; }

        string TelemetryClientTransport { get; }

        /// <summary>
        /// Send the buffer.
        /// Must be thread safe as it is called to send metrics and the telemetry.
        /// </summary>
        bool Send(byte[] buffer, int length);
    }
}