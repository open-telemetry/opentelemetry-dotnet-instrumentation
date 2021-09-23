using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace IntegrationTests.Helpers
{
    /// <summary>
    /// Drains the standard and error output of a process
    /// </summary>
    public class ProcessHelper : IDisposable
    {
        private const int DefaultTimeoutMinutes = 30;

        private readonly ManualResetEventSlim _outputMutex = new();
        private readonly StringBuilder _outputBuffer = new();
        private readonly StringBuilder _errorBuffer = new();

        private bool _isStdOutputDrained;
        private bool _isErrOutputDrained;
        private object _outputLock = new object();

        public ProcessHelper(Process process)
        {
            process.OutputDataReceived += (_, e) => DrainOutput(e.Data, _outputBuffer, isErrorStream: false);
            process.ErrorDataReceived += (_, e) => DrainOutput(e.Data, _errorBuffer, isErrorStream: true);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public string StandardOutput => _outputBuffer.ToString();

        public string ErrorOutput => _errorBuffer.ToString();

        public bool Drain()
        {
            return Drain(TimeSpan.FromMinutes(DefaultTimeoutMinutes));
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
    }
}
