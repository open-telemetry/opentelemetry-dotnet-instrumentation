namespace OpenTelemetry.AutoInstrumentation.DuckTyping
{
    /// <summary>
    /// Duck attribute where the underlying member is a field
    /// </summary>
    public class DuckFieldAttribute : DuckAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuckFieldAttribute"/> class.
        /// </summary>
        public DuckFieldAttribute()
        {
            Kind = DuckKind.Field;
        }
    }
}
