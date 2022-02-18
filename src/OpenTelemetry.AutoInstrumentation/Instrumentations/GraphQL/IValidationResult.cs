namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

/// <summary>
/// GraphQL.Validation.IValidationResult interface for ducktyping
/// </summary>
public interface IValidationResult
{
    /// <summary>
    /// Gets the execution errors
    /// </summary>
    IExecutionErrors Errors { get; }
}
