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

        private readonly ManualResetEventSlim _errorMutex = new();
        private readonly ManualResetEventSlim _outputMutex = new();
        private readonly StringBuilder _outputBuffer = new();
        private readonly StringBuilder _errorBuffer = new();

        public ProcessHelper(Process process)
        {
            process.OutputDataReceived += (_, e) => DrainOutput(e, _outputBuffer, _outputMutex);
            process.ErrorDataReceived += (_, e) => DrainOutput(e, _errorBuffer, _errorMutex);

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
            return _outputMutex.Wait(timeout) && _errorMutex.Wait(timeout);
        }

        public void Dispose()
        {
            _errorMutex.Dispose();
            _outputMutex.Dispose();
        }

        private static void DrainOutput(DataReceivedEventArgs e, StringBuilder buffer, ManualResetEventSlim mutex)
        {
            string data = e.Data;
            if (data == null)
            {
                mutex.Set();
                return;
            }

            buffer.AppendLine(data);
        }
    }
}
