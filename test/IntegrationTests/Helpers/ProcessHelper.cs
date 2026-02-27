// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text;

namespace IntegrationTests.Helpers;

/// <summary>
/// Drains the standard and error output of a process
/// </summary>
internal sealed class ProcessHelper : IDisposable
{
    private readonly ManualResetEventSlim _outputMutex = new();
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();
    private readonly object _outputLock = new();

    private bool _isStdOutputDrained;
    private bool _isErrOutputDrained;

    public ProcessHelper(Process? process)
    {
        if (process == null)
        {
            return;
        }

        // If this is our special process type and it already has a helper...
        if (process is InstrumentedProcessHelper.IntegrationTestProcess { AttachedHelper: not null } it)
        {
            // "Adopt" the state of the existing helper instead of starting a new one
            Process = it.AttachedHelper.Process;
            _outputBuffer = it.AttachedHelper._outputBuffer;
            _errorBuffer = it.AttachedHelper._errorBuffer;
            _outputMutex = it.AttachedHelper._outputMutex;
            // Note: In this case, don't call BeginOutputReadLine()/BeginErrorReadLine() again!
            return;
        }

        Process = process;
        Process.OutputDataReceived += (_, e) => DrainOutput(e.Data, _outputBuffer, isErrorStream: false);
        Process.ErrorDataReceived += (_, e) => DrainOutput(e.Data, _errorBuffer, isErrorStream: true);

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }

    public Process? Process { get; }

    public string StandardOutput => CompleteOutput(_outputBuffer);

    public string ErrorOutput => CompleteOutput(_errorBuffer);

    public bool Drain()
    {
        return Drain(TestTimeout.ProcessExit);
    }

    public bool Drain(TimeSpan timeout)
    {
        return _outputMutex.Wait(timeout);
    }

    public void Dispose()
    {
        _outputMutex.Dispose();
    }

    private void DrainOutput(string? data, StringBuilder buffer, bool isErrorStream)
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
        if (Process == null || Process.HasExited)
        {
            return builder.ToString();
        }

        throw new InvalidOperationException("Process is still running and not ready to be drained.");
    }
}
