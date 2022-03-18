// <copyright file="ProcessHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
