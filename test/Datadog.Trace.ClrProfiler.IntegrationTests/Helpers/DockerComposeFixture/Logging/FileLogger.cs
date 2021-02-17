using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DockerComposeFixture.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string logfileName;

        public FileLogger(string logfileName)
        {
            if (logfileName != null)
            {
                if (File.Exists(logfileName))
                {
                    File.Delete(logfileName);
                }
            }
            this.logfileName = logfileName;
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
            using (var stream = new FileStream(this.logfileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(msg);
                writer.Flush();
                writer.Close();
            }

        }
    }
}
