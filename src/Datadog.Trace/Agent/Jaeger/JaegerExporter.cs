using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datadog.Trace.Logging;
using Thrift.Protocol;
using Thrift.Transport;

namespace Datadog.Trace.Agent.Jaeger
{
    internal class JaegerExporter : IExporter
    {
        internal const int DefaultMaxPayloadSizeInBytes = 4096;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(JaegerExporter));

        private readonly int _maxPayloadSizeInBytes;
        private readonly string _serviceName;
        private readonly TTransport _clientTransport;
        private readonly JaegerThriftClient _thriftClient;
        private readonly InMemoryTransport _memoryTransport;
        private readonly TProtocol _memoryProtocol;
        private readonly Uri _agentUri;
        private int _batchByteSize;

        public JaegerExporter(Uri agentUri, string serviceName, int? maxPayloadSizeInBytes = null, IJaegerClient jaegerClient = null)
        {
            var protocolFactory = new TCompactProtocol.Factory();

            _maxPayloadSizeInBytes = maxPayloadSizeInBytes ?? DefaultMaxPayloadSizeInBytes;
            _clientTransport = new JaegerThriftClientTransport(agentUri.Host, agentUri.Port, stream: null, client: jaegerClient);
            _thriftClient = new JaegerThriftClient(protocolFactory.GetProtocol(_clientTransport));
            _memoryTransport = new InMemoryTransport(16000);
            _memoryProtocol = protocolFactory.GetProtocol(_memoryTransport);
            _agentUri = agentUri ?? throw new ArgumentNullException(nameof(agentUri));
            _serviceName = serviceName;

            Process = new Process(serviceName);
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
                Log.Debug("Exception sending traces to {0}: {1}", _agentUri, ex.Message);

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
