using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// TopicPartition interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITopicPartition
    {
        /// <summary>
        ///     Gets the Kafka topic name.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the Kafka partition.
        /// </summary>
        public Partition Partition { get; }
    }
}
