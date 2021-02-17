using DockerComposeFixture.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DockerComposeFixture.Compose
{
    public interface IDockerCompose
    {
        void Init(string dockerComposeArgs, string dockerComposeUpArgs, string dockerComposeDownArgs);
        void Down();
        IEnumerable<string> Ps();
        Task Up();
        int PauseMs { get; }
        ILogger[] Logger { get; }
    }
}