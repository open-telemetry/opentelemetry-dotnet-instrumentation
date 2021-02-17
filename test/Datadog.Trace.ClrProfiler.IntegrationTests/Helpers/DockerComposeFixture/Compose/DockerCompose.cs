using DockerComposeFixture.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerComposeFixture.Compose
{
    public class DockerCompose : IDockerCompose
    {
        public DockerCompose(ILogger[] logger)
        {
            this.Logger = logger;
        }
        private string dockerComposeArgs,dockerComposeUpArgs, dockerComposeDownArgs;

        public void Init(string dockerComposeArgs, string dockerComposeUpArgs, string dockerComposeDownArgs)
        {
            this.dockerComposeArgs = dockerComposeArgs;
            this.dockerComposeUpArgs = dockerComposeUpArgs;
            this.dockerComposeDownArgs = dockerComposeDownArgs;
        }

        public Task Up()
        {
            var start = new ProcessStartInfo("docker-compose", $"{this.dockerComposeArgs} up {this.dockerComposeUpArgs}");
            return Task.Run(() =>  this.RunProcess(start) );
        }

        private void RunProcess(ProcessStartInfo processStartInfo)
        {
            var runner = new ProcessRunner(processStartInfo);
            foreach (var logger in this.Logger)
            {
                runner.Subscribe(logger);
            }
            runner.Execute();
        }

        public int PauseMs => 1000;
        public ILogger[] Logger { get; }

        public void Down()
        {
            var down = new ProcessStartInfo("docker-compose", $"{this.dockerComposeArgs} down {this.dockerComposeDownArgs}");
            this.RunProcess(down);
        }

        public IEnumerable<string> Ps()
        {
            var ps = new ProcessStartInfo("docker-compose", $"{this.dockerComposeArgs} ps");
            var runner = new ProcessRunner(ps);
            var observerToQueue = new ObserverToQueue<string>();

            foreach (var logger in this.Logger)
            {
                runner.Subscribe(logger);
            }
            runner.Subscribe(observerToQueue);
            runner.Execute();
            return observerToQueue.Queue.ToArray();
        }
    }
}