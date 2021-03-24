namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Enumeration for the available propagator types.
    /// </summary>
    public enum PropagatorType
    {
        /// <summary>
        /// The default propagator.
        /// Default is <c>Datadog</c>.
        /// </summary>
        Default,

        /// <summary>
        /// The Datadog propagator.
        /// </summary>
        Datadog,

        /// <summary>
        /// The B3 propagator
        /// </summary>
        B3
    }
}
