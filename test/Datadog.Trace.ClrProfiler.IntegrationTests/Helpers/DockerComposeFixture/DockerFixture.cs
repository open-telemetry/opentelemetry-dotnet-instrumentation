using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DockerComposeFixture.Compose;
using DockerComposeFixture.Exceptions;
using DockerComposeFixture.Logging;
using Xunit.Abstractions;

namespace DockerComposeFixture
{
    public class DockerFixture : IDisposable
    {
        private IDockerCompose dockerCompose;
        private Func<string[], bool> customUpTest;
        private bool initialised;
        private ILogger[] loggers;
        private int startupTimeoutSecs;
        private readonly ITestOutputHelper output = null;

        // public DockerFixture(ITestOutputHelper output)
        // {
        //     this.output = output;
        // }

        /// <summary>
        /// Initialize docker compose services from file(s) but only once.
        /// If you call this multiple times on the same DockerFixture then it will be ignored.
        /// </summary>
        /// <param name="setupOptions">Options that control how docker-compose is executed.</param>
        public void InitOnce(Func<IDockerFixtureOptions> setupOptions)
        {
            InitOnce(setupOptions, null);
        }

        /// <summary>
        /// Initialize docker compose services from file(s) but only once.
        /// If you call this multiple times on the same DockerFixture then it will be ignored.
        /// </summary>
        /// <param name="setupOptions">Options that control how docker-compose is executed.</param>
        /// <param name="dockerCompose"></param>
        public void InitOnce(Func<IDockerFixtureOptions> setupOptions, IDockerCompose dockerCompose)
        {
            if (!this.initialised)
            {
                this.Init(setupOptions, dockerCompose);
                this.initialised = true;
            }
        }


        /// <summary>
        /// Initialize docker compose services from file(s).
        /// </summary>
        /// <param name="setupOptions">Options that control how docker-compose is executed</param>
        public void Init(Func<IDockerFixtureOptions> setupOptions)
        {
            Init(setupOptions, null);
        }

        /// <summary>
        /// Initialize docker compose services from file(s).
        /// </summary>
        /// <param name="setupOptions">Options that control how docker-compose is executed</param>
        /// <param name="compose"></param>
        public void Init(Func<IDockerFixtureOptions> setupOptions, IDockerCompose compose)
        {
            var options = setupOptions();
            options.Validate();
            string logFile = options.DebugLog
                ? Path.Combine(Path.GetTempPath(), $"docker-compose-{DateTime.Now.Ticks}.log")
                : null;

            this.Init(options.DockerComposeFiles, options.DockerComposeUpArgs, options.DockerComposeDownArgs,
                options.StartupTimeoutSecs, options.CustomUpTest, compose, this.GetLoggers(logFile).ToArray());
        }

        private IEnumerable<ILogger> GetLoggers(string file)
        {
            yield return new ListLogger();
            yield return new ConsoleLogger();
            if (this.output != null)
            {
                yield return new XUnitLogger(this.output);
            }
            if (!string.IsNullOrEmpty(file))
            {
                yield return new FileLogger(file);
            }
        }

        /// <summary>
        /// Initialize docker compose services from file(s).
        /// </summary>
        /// <param name="dockerComposeFiles">Array of docker compose files</param>
        /// <param name="dockerComposeUpArgs">Arguments to append after 'docker-compose -f file.yml up'</param>
        /// <param name="dockerComposeDownArgs">Arguments to append after 'docker-compose -f file.yml down'</param>
        /// <param name="startupTimeoutSecs">How long to wait for the application to start before giving up</param>
        /// <param name="customUpTest">Checks whether the docker-compose services have come up correctly based upon the output of docker-compose</param>
        /// <param name="dockerCompose"></param>
        /// <param name="logger"></param>
        public void Init(string[] dockerComposeFiles, string dockerComposeUpArgs, string dockerComposeDownArgs,
            int startupTimeoutSecs, Func<string[], bool> customUpTest = null,
            IDockerCompose dockerCompose = null, ILogger[] logger = null)
        {
            this.loggers = logger ?? GetLoggers(null).ToArray();

            var dockerComposeFilePaths = dockerComposeFiles.Select(this.GetComposeFilePath);
            this.dockerCompose = dockerCompose ?? new DockerCompose(this.loggers);
            this.customUpTest = customUpTest;
            this.startupTimeoutSecs = startupTimeoutSecs;

            this.dockerCompose.Init(
                string.Join(" ",
                        dockerComposeFilePaths
                            .Select(f => $"-f \"{f}\""))
                    .Trim(), dockerComposeUpArgs, dockerComposeDownArgs);
            this.Start();
        }

        private string GetComposeFilePath(string file)
        {
            if (File.Exists(file))
            {
                return file;
            }

            DirectoryInfo curDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            if (File.Exists(Path.Combine(curDir.FullName, file)))
            {
                return Path.Combine(curDir.FullName, file);
            }

            if (!file.Contains(Path.DirectorySeparatorChar))
            {
                while (curDir != null)
                {
                    string curFile = Path.Combine(curDir.FullName, file);
                    if (File.Exists(curFile))
                    {
                        return curFile;
                    }
                    curDir = curDir.Parent;
                }
            }
            throw new ArgumentException($"The file {file} was not found in {AppDomain.CurrentDomain.BaseDirectory} or any parent directories");
        }

        /// <summary>
        /// Kills all running docker containers if their name contains applicationName
        /// </summary>
        /// <param name="applicationName">Name to match against</param>
        /// <param name="killEverything">Optionally kill all docker containers</param>
        /// <returns></returns>
        public static async Task Kill(string applicationName, bool killEverything = false)
        {
            await Kill(new Regex(Regex.Escape(applicationName)), killEverything);
        }

        /// <summary>
        /// Kills all running docker containers if their name matches a regex
        /// </summary>
        /// <param name="filterRx">Regex to match against</param>
        /// <param name="killEverything">Optionally kill all docker containers</param>
        /// <returns></returns>
        public static async Task Kill(Regex filterRx, bool killEverything = false)
        {
            Process ps = Process.Start(new ProcessStartInfo("docker", "ps")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            ps.WaitForExit();

            var ids = (await ps.StandardOutput.ReadToEndAsync())
                .Split('\n')
                .Skip(1)
                .Where(s => killEverything || filterRx.IsMatch(s))
                .Select(s => Regex.Match(s, @"^[\da-f]+").Value)
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var id in ids)
            {
                Process.Start("docker", $"kill {id}").WaitForExit();
            }

        }

        public virtual void Dispose()
        {
            this.Stop();
        }

        private void Start()
        {
            if (this.CheckIfRunning().hasContainers)
            {
                this.loggers.Log("---- stopping already running docker services ----");
                this.Stop();
            }

            this.loggers.Log("---- starting docker services ----");
            var upTask = this.dockerCompose.Up();

            for (int i = 0; i < this.startupTimeoutSecs; i++)
            {
                if (upTask.IsCompleted)
                {
                    this.loggers.Log("docker-compose exited prematurely");
                    break;
                }
                this.loggers.Log($"---- checking docker services ({i + 1}/{this.startupTimeoutSecs}) ----");
                Thread.Sleep(this.dockerCompose.PauseMs);
                if (this.customUpTest != null)
                {
                    if (this.customUpTest(this.loggers.GetLoggedLines()))
                    {
                        this.loggers.Log("---- custom up test satisfied ----");
                        return;
                    }
                }
                else
                {
                    var (hasContainers, containersAreUp) = this.CheckIfRunning();
                    if (hasContainers && containersAreUp)
                    {
                        this.loggers.Log("---- docker services are up ----");
                        return;
                    }
                }
            }
            throw new DockerComposeException(this.loggers.GetLoggedLines());
        }

        private (bool hasContainers, bool containersAreUp) CheckIfRunning()
        {
            var lines = this.dockerCompose.Ps().ToList()
                .Where(l => l != null)
                .SkipWhile(l => !l.Contains("--------"))
                .Skip(1)
                .ToList();
            return (
                lines.Any(),
                lines.Count(l => Regex.IsMatch(l, @"\s+Up\s+")) == lines.Count());
        }

        private void Stop()
        {
            this.dockerCompose.Down();
        }


    }
}
