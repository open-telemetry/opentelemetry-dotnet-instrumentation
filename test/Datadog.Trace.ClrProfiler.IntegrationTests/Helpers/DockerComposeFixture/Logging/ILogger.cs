using System;
using System.Collections.Generic;

namespace DockerComposeFixture.Logging
{
    public interface ILogger : IObserver<string>
    {
        void Log(string msg);
    }
}