using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DockerComposeFixture.Compose
{
    public class ProcessRunner : IObservable<string>
    {
        private readonly List<IObserver<string>> observers;
        private readonly ProcessStartInfo startInfo;

        public ProcessRunner(ProcessStartInfo processStartInfo)
        {
            this.observers = new List<IObserver<string>>();
            this.startInfo = processStartInfo;
        }

        public void Execute()
        {
            void LogData(object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    this.observers.ForEach(o => o.OnNext(e.Data));
                }
            }

            var process = new Process { StartInfo = this.startInfo };
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += LogData;
            process.ErrorDataReceived += LogData;

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();

            this.observers.ForEach(o => o.OnCompleted());
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            if (!this.observers.Contains(observer))
            {
                this.observers.Add(observer);
            }

            return new Unsubscriber(this.observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<string>> observers;
            private readonly IObserver<string> observer;

            public Unsubscriber(List<IObserver<string>> observers, IObserver<string> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (this.observer != null && this.observers.Contains(this.observer))
                    this.observers.Remove(this.observer);
            }
        }

    }
}