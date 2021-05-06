namespace Datadog.Trace.Conventions
{
    /// <summary>
    /// Convention used when defining format of TraceId.
    /// </summary>
    public interface ITraceIdConvention
    {
        /// <summary>
        /// Generates new unique trace id based on convention.
        /// </summary>
        /// <returns>Trace id.</returns>
        TraceId GenerateNewTraceId();

        /// <summary>
        /// Creates new trace id based on given string.
        /// </summary>
        /// <param name="id">String of id.</param>
        /// <returns>Trace id.</returns>
        TraceId CreateFromString(string id);
    }
}
