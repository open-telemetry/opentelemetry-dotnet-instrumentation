using System;
using System.Collections.Generic;

namespace DockerComposeFixture
{
    public interface IDockerFixtureOptions
    {
        Func<string[], bool> CustomUpTest { get; set; }
        string[] DockerComposeFiles { get; set; }
        bool DebugLog { get; set; }
        string DockerComposeUpArgs { get; set; }
        string DockerComposeDownArgs { get; set; }
        int StartupTimeoutSecs { get; set; }

        void Validate();

    }
}