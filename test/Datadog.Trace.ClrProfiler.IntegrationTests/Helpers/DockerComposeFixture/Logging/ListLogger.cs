using System;
using System.Collections.Generic;
using System.Text;

namespace DockerComposeFixture.Logging
{
    public class ListLogger : ILogger
    {
        public ListLogger()
        {
            this.LoggedLines = new List<string>();
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
            this.LoggedLines.Add(msg);
        }

        public List<string> LoggedLines { get; }
    }
}
