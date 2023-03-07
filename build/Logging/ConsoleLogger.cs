using NuGet.Common;

namespace Logging;

internal class ConsoleLogger : ILogger
{
    public void LogDebug(string data)
    {
        Log(LogLevel.Debug, data);
    }

    public void LogVerbose(string data)
    {
        Log(LogLevel.Verbose, data);
    }

    public void LogInformation(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void LogMinimal(string data)
    {
        Log(LogLevel.Minimal, data);
    }

    public void LogWarning(string data)
    {
        Log(LogLevel.Warning, data);
    }

    public void LogError(string data)
    {
        Log(LogLevel.Error, data);
    }

    public void LogInformationSummary(string data)
    {
        Console.WriteLine($"[Summary] {data}");
    }

    public void Log(LogLevel level, string data)
    {
        Console.WriteLine($"[{level}] {data}");
    }

    public async Task LogAsync(LogLevel level, string data)
    {
        await Task.Run(() => Log(level, data))
                  .ConfigureAwait(false);
    }

    public void Log(ILogMessage message)
    {
        Log(message.Level, message.Message);
    }

    public async Task LogAsync(ILogMessage message)
    {
        await Task.Run(() => Log(message.Level, message.Message))
                  .ConfigureAwait(false);
    }
}
