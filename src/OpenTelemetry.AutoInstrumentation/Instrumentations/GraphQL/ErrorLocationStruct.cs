using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL
{
    /// <summary>
    /// GraphQL.ErrorLocation interface for ducktyping
    /// </summary>
    [DuckCopy]
    public struct ErrorLocationStruct
    {
        /// <summary>
        /// Gets the line number of the document where the error occurred
        /// </summary>
        public int Line;

        /// <summary>
        /// Gets the column number of the document where the error occurred
        /// </summary>
        public int Column;
    }
}
