using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DockerComposeFixture.Logging
{
    public class ConsoleLogger : ILogger
    {
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
            Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }
        
    }
}
