namespace Datadog.Trace.Configuration.Types
{
    /// <summary>
    /// Contains default available propagator types.
    /// </summary>
    public static class PropagatorTypes
    {
        /// <summary>
        /// The Datadog propagator.
        /// </summary>
        public const string Datadog = "Datadog";

        /// <summary>
        /// The B3 propagator
        /// </summary>
        public const string B3 = "B3";

        /// <summary>
        /// The W3C propagator
        /// </summary>
        public const string W3C = "W3C";
    }
}
