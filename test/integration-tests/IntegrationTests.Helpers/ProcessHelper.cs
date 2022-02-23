using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace IntegrationTests.Helpers;

/// <summary>
/// Drains the standard and error output of a process
/// </summary>
public class ProcessHelper : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private readonly ManualResetEventSlim _outputMutex = new();
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();
    private readonly Process _process;

    private bool _isStdOutputDrained;
    private bool _isErrOutputDrained;
    private object _outputLock = new object();

    public ProcessHelper(Process process)
    {
        _process = process;
        _process.OutputDataReceived += (_, e) => DrainOutput(e.Data, _outputBuffer, isErrorStream: false);
        _process.ErrorDataReceived += (_, e) => DrainOutput(e.Data, _errorBuffer, isErrorStream: true);

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public string StandardOutput => CompleteOutput(_outputBuffer);

    public string ErrorOutput => CompleteOutput(_errorBuffer);

    public bool Drain()
    {
        return Drain(DefaultTimeout);
    }

    public bool Drain(TimeSpan timeout)
    {
        return _outputMutex.Wait(timeout);
    }

    public void Dispose()
    {
        _outputMutex.Dispose();
    }

    private void DrainOutput(string data, StringBuilder buffer, bool isErrorStream)
    {
        if (data != null)
        {
            buffer.AppendLine(data);
            return;
        }

        lock (_outputLock)
        {
            if (isErrorStream)
            {
                _isErrOutputDrained = true;
            }
            else
            {
                _isStdOutputDrained = true;
            }

            if (_isStdOutputDrained && _isErrOutputDrained)
            {
                _outputMutex.Set();
            }
        }
    }

    private string CompleteOutput(StringBuilder builder)
    {
        if (_process.HasExited)
        {
            return builder.ToString();
        }

        throw new InvalidOperationException("Process is still running and not ready to be drained.");
    }
}
