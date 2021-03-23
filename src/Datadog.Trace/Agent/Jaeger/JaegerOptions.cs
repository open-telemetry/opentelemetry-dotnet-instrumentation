namespace Datadog.Trace.Agent.Jaeger
{
    /// <summary>
    /// Contains Jaeger exporter settings.
    /// </summary>
    public class JaegerOptions
    {
        internal const int DefaultMaxPayloadSizeInBytes = 4096;

        /// <summary>
        /// Gets or sets the Service name.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the Jaeger agent host. Default value: localhost.
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the Jaeger agent "compact thrift protocol" port. Default value: 6831.
        /// </summary>
        public int Port { get; set; } = 6831;

        /// <summary>
        /// Gets or sets the maximum payload size in bytes. Default value: 4096.
        /// </summary>
        public int MaxPayloadSizeInBytes { get; set; } = DefaultMaxPayloadSizeInBytes;

        /// <summary>
        /// Gets or sets the network stack exporter. Default exporter: JaegerUdpClient.
        /// </summary>
        internal IJaegerClient TransportClient { get; set; } = new JaegerUdpClient();
    }
}
