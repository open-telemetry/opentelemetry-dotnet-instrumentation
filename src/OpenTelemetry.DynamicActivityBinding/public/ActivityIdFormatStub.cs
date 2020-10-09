namespace OpenTelemetry.DynamicActivityBinding
{
    /// <summary>
    /// Specifies the format of the Id property.
    /// The values need to map exactly to
    /// https://github.com/dotnet/runtime/blob/db6c3205edb3ad2fd7bc2b4a77bbaadbf3c95945/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/Activity.cs#L1513
    /// </summary>
    public enum ActivityIdFormatStub
    {
        /// <summary>
        /// An unknown format.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The hierarchical format.
        /// See https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format.
        /// </summary>
        Hierarchical = 1,

        /// <summary>
        /// The W3C format.
        /// See https://w3c.github.io/trace-context/.
        /// </summary>
        W3C = 2,
    }
}
