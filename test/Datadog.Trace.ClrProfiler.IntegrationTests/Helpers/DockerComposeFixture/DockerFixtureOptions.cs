using System;
using System.Collections.Generic;
using System.Text;

namespace DockerComposeFixture
{
    /// <summary>
    /// Options that control how docker-compose is executed
    /// </summary>
    public class DockerFixtureOptions : IDockerFixtureOptions
    {
        /// <summary>
        /// Checks whether the docker-compose services have come up correctly based upon the output of docker-compose
        /// </summary>
        public Func<string[], bool> CustomUpTest { get; set; }

        /// <summary>
        /// Array of docker compose files
        /// Files are converted into the arguments '-f file1 -f file2 etc'
        /// Default is 'docker-compose.yml'
        /// </summary>
        public string[] DockerComposeFiles { get; set; } = new[] { "docker-compose.yml" };
        /// <summary>
        /// When true this logs docker-compose output to %temp%\docker-compose-*.log
        /// </summary>
        public bool DebugLog { get; set; }
        /// <summary>
        /// Arguments to append after 'docker-compose -f file.yml up'
        /// Default is 'docker-compose -f file.yml up' you can append '--build' if you want it to always build
        /// </summary>
        public string DockerComposeUpArgs { get; set; } = "";
        /// <summary>
        /// Arguments to append after 'docker-compose -f file.yml down'
        /// Default is 'docker-compose -f file.yml down --remove-orphans' you can add '--rmi all' if you want to guarantee a fresh build on each test
        /// </summary>
        public string DockerComposeDownArgs { get; set; } = "--remove-orphans";

        /// <summary>
        /// How many seconds to wait for the application to start before giving up. (Default is 120.)
        /// </summary>
        public int StartupTimeoutSecs { get; set; } = 120;

        public void Validate()
        {
            if (this.StartupTimeoutSecs < 1)
            {
                throw new ArgumentException(nameof(this.StartupTimeoutSecs));
            }
            if (this.DockerComposeFiles == null
                || this.DockerComposeFiles.Length == 0)
            {
                throw new ArgumentException(nameof(this.DockerComposeFiles));
            }
        }


    }
}
