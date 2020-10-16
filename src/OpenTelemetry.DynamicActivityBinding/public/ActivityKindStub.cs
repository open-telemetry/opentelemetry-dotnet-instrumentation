namespace OpenTelemetry.DynamicActivityBinding
{
    /// <summary>
    /// Describes the relationship between the activity, its parents and its children in a trace.
    /// This MUST have a 1-to-1 value mapping to
    /// https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/ActivityKind.cs
    /// </summary>
    public enum ActivityKindStub
    {
        /// <summary>
        /// Internal operation within an application, as opposed to operations with remote parents or children.
        /// This is the default value.
        /// </summary>
        Internal = 0,

        /// <summary>
        /// Requests incoming from external component.
        /// </summary>
        Server = 1,

        /// <summary>
        /// Outgoing request to the external component.
        /// </summary>
        Client = 2,

        /// <summary>
        /// Output provided to external components.
        /// </summary>
        Producer = 3,

        /// <summary>
        /// Output received from an external component.
        /// </summary>
        Consumer = 4,
    }
}
