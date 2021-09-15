using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Thrift.Protocol;
using Thrift.Transport;

namespace Datadog.Trace.Agent.Jaeger
{
    internal class JaegerExporter : IExporter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(JaegerExporter));

        private readonly int _maxPayloadSizeInBytes;
        private readonly string _serviceName;
        private readonly TTransport _clientTransport;
        private readonly JaegerOptions _options;
        private readonly JaegerThriftClient _thriftClient;
        private readonly InMemoryTransport _memoryTransport;
        private readonly TProtocol _memoryProtocol;

        private int _batchByteSize;

        public JaegerExporter(JaegerOptions options)
        {
            var protocolFactory = new TCompactProtocol.Factory();

            _options = options;
            _maxPayloadSizeInBytes = options.MaxPayloadSizeInBytes;
            _clientTransport = new JaegerThriftClientTransport(options.Host, options.Port, new MemoryStream(), options.TransportClient);
            _thriftClient = new JaegerThriftClient(protocolFactory.GetProtocol(_clientTransport));
            _memoryTransport = new InMemoryTransport(16000);
            _memoryProtocol = protocolFactory.GetProtocol(_memoryTransport);
            _serviceName = options.ServiceName;

            Process = new Process(_serviceName);
        }

        internal Process Process { get; set; }

        internal Batch Batch { get; private set; }

        public Task<bool> SendTracesAsync(Span[][] traces)
        {
            if (traces == null || traces.Length == 0)
            {
                // Nothing to send, no ping for Zipkin.
                return Task.FromResult(true);
            }

            try
            {
                foreach (var trace in traces)
                {
                    if (Batch == null)
                    {
                        SetResourceAndInitializeBatch(_serviceName);
                    }

                    foreach (var span in trace)
                    {
                        AppendSpan(span.ToJaegerSpan());
                    }

                    SendCurrentBatch();
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Debug("Exception sending traces to {0}: {1}", $"{_options.Host}:{_options.Port}", ex.Message);

                return Task.FromResult(false);
            }
        }

        internal void SetResourceAndInitializeBatch(string serviceName, string serviceNamespace = null)
        {
            var process = Process;

            if (process.Tags == null)
            {
                process.Tags = new Dictionary<string, JaegerTag>();
            }

            if (serviceName != null)
            {
                serviceName = string.IsNullOrEmpty(serviceNamespace)
                    ? serviceName
                    : serviceNamespace + "." + serviceName;
            }

            if (!string.IsNullOrEmpty(serviceName))
            {
                process.ServiceName = serviceName;
            }

            Process.Message = BuildThriftMessage(Process).ToArray();
            Batch = new Batch(Process);

            _batchByteSize = Process.Message.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AppendSpan(JaegerSpan jaegerSpan)
        {
            var spanMessage = BuildThriftMessage(jaegerSpan);

            jaegerSpan.Return();

            var spanTotalBytesNeeded = spanMessage.Count;

            if (_batchByteSize + spanTotalBytesNeeded >= _maxPayloadSizeInBytes)
            {
                SendCurrentBatch();
            }

            Batch.Add(spanMessage);

            _batchByteSize += spanTotalBytesNeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BufferWriterMemory BuildThriftMessage(Process process)
        {
            process.Write(_memoryProtocol);

            return _memoryTransport.ToBuffer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BufferWriterMemory BuildThriftMessage(in JaegerSpan jaegerSpan)
        {
            jaegerSpan.Write(_memoryProtocol);

            return _memoryTransport.ToBuffer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendCurrentBatch()
        {
            try
            {
                _thriftClient.SendBatch(Batch);
            }
            finally
            {
                Batch.Clear();

                _batchByteSize = Process.Message.Length;
                _memoryTransport.Reset();
            }
        }
    }
}
