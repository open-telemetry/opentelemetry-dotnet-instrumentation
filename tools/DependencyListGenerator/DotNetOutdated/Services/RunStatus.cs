namespace DependencyListGenerator.DotNetOutdated.Services;

public class RunStatus
{
    public RunStatus(string output, string errors, int exitCode)
    {
        Output = output;
        Errors = errors;
        ExitCode = exitCode;
    }

    public string Output { get; }

    public string Errors { get; }

    public int ExitCode { get; }

    public bool IsSuccess => ExitCode == 0;
}
