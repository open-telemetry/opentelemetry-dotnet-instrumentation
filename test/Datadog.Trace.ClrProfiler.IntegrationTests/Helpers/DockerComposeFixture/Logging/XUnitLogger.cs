using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DockerComposeFixture.Logging
{
    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper xlogOutput;

        public XUnitLogger(ITestOutputHelper xlogOutput)
        {
            this.xlogOutput = xlogOutput;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {
            this.Log(error.Message + "\n" + error.StackTrace);
            throw error;
        }


        public void OnNext(string value)
        {
            this.Log(value);
        }

        public void Log(string msg)
        {
            this.xlogOutput.WriteLine(msg);
        }

    }
}