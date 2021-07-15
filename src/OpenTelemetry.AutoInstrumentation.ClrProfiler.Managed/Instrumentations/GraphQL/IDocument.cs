namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Instrumentations.GraphQL
{
    /// <summary>
    /// GraphQL.Language.AST.Document interface for ducktyping
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// Gets the original query from the document
        /// </summary>
        string OriginalQuery { get; }
    }
}
